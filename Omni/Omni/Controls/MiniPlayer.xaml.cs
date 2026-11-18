using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibVLCSharp.Shared;
using Omni.Data;
using Omni.Pages;
using VlcPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace Omni.Controls
{
    // Small music controls at the bottom of the sidebar. They drive the Music page's shared
    // player, so the song keeps playing and can be controlled from any page. Hidden while
    // nothing is playing, and on the Music page itself, which has the full controls.
    public partial class MiniPlayer : UserControl
    {
        private const string PlayGlyph = "";
        private const string PauseGlyph = "";

        private readonly OmniService _service = new();
        private VlcPlayer? _player;
        private bool _onMusicPage;
        // True while the user is dragging the seek bar, so playback doesn't move it
        private bool _seeking;
        // The song whose details and cover are shown, so they're only read once per song
        private string? _shownPath;
        // Opening a song resets the player's volume; this puts it back once it plays
        private int _volumeToRestore = -1;

        public MiniPlayer()
        {
            InitializeComponent();
        }

        // Called whenever the main window changes page
        public void Refresh(bool onMusicPage)
        {
            _onMusicPage = onMusicPage;
            Attach();
            UpdateDisplay();
            // The song may have been (un)favourited on the Music page
            UpdateFavourite();
            // The Music page may have changed the volume; the bar and icon pick it up here
            if (_player?.Volume is >= 0 and var volume)
                ShowVolume(volume);
        }

        // The player only exists once the Music page has been opened, so attach when it does
        private void Attach()
        {
            if (_player is not null || MusicPage.SharedPlayer is not { } player) return;

            _player = player;
            // LibVLC raises these on its own thread
            _player.Playing += OnPlayerChanged;
            _player.Paused += OnPlayerChanged;
            _player.Stopped += OnPlayerChanged;
            _player.EndReached += OnEndReached;
            _player.TimeChanged += OnTimeChanged;
            _player.LengthChanged += OnPlayerChanged;
        }

        private void MiniPlayer_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_player is null) return;

            _player.Playing -= OnPlayerChanged;
            _player.Paused -= OnPlayerChanged;
            _player.Stopped -= OnPlayerChanged;
            _player.EndReached -= OnEndReached;
            _player.TimeChanged -= OnTimeChanged;
            _player.LengthChanged -= OnPlayerChanged;
            _player = null;
        }

        // ----- Player events (marshalled to the UI thread) -----

        private void OnPlayerChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() =>
        {
            if (_player is not null && _volumeToRestore >= 0 && _player.IsPlaying)
            {
                _player.Volume = _volumeToRestore;
                _volumeToRestore = -1;
            }
            UpdateDisplay();
        });

        private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            long time = e.Time;
            Dispatcher.BeginInvoke(() =>
            {
                if (_player is null || Visibility != Visibility.Visible) return;

                CurrentTimeText.Text = FormatTime(time);
                if (!_seeking)
                    SeekSlider.Value = _player.Position;
            });
        }

        // Away from the Music page, carry on with the next song in the playlist
        // (on the Music page, the page itself does this)
        private void OnEndReached(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() =>
        {
            if (_onMusicPage || !PlayRelative(1))
                UpdateDisplay();
        });

        // ----- Display -----

        private void UpdateDisplay()
        {
            string? path = CurrentPath();
            bool active = _player?.State is VLCState.Playing or VLCState.Paused or VLCState.Opening or VLCState.Buffering;
            Visibility = active && path is not null && !_onMusicPage ? Visibility.Visible : Visibility.Collapsed;
            if (Visibility != Visibility.Visible || _player is null || path is null) return;

            if (!string.Equals(path, _shownPath, StringComparison.OrdinalIgnoreCase))
            {
                _shownPath = path;
                var (title, details) = MusicPage.ReadTrackInfo(path);
                TitleText.Text = title;
                DetailsText.Text = details;
                DetailsText.Visibility = details.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                ToolTip = details.Length > 0 ? $"{title}\n{details}" : title;

                var cover = ReadCover(path);
                Brush? brush = cover is null ? null : new ImageBrush(cover) { Stretch = Stretch.UniformToFill };
                Cover.Background = brush;
                Backdrop.Background = brush;
                UpdateFavourite();
            }

            bool playing = _player.IsPlaying || _player.State is VLCState.Opening or VLCState.Buffering;
            PlayPauseButton.Content = playing ? PauseGlyph : PlayGlyph;
            PlayPauseButton.ToolTip = playing ? "Pause" : "Play";
            TotalTimeText.Text = FormatTime(_player.Length);
            CurrentTimeText.Text = FormatTime(_player.Time);
            if (!_seeking)
                SeekSlider.Value = Math.Max(0, _player.Position);
        }

        private string? CurrentPath()
        {
            using Media? media = _player?.Media;
            return media is null ? null : new Uri(media.Mrl).LocalPath;
        }

        // The song's embedded cover, or null when it has none or can't be read
        private static ImageSource? ReadCover(string path)
        {
            try
            {
                using var file = TagLib.File.Create(path);
                var picture = file.Tag.Pictures.FirstOrDefault();
                if (picture is null) return null;

                using var stream = new MemoryStream(picture.Data.Data);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = 240;   // it's shown small, so don't decode a full-size scan
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception ex) when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException
                                           or IOException or NotSupportedException or FormatException)
            {
                return null;
            }
        }

        private static string FormatTime(long milliseconds)
        {
            var t = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
            return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
        }

        // ----- Controls -----

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_player is null) return;

            if (_player.IsPlaying) _player.SetPause(true);
            else if (_player.State == VLCState.Paused) _player.SetPause(false);
        }

        private void Previous_Click(object sender, RoutedEventArgs e) => PlayRelative(-1);

        private void Next_Click(object sender, RoutedEventArgs e) => PlayRelative(1);

        // Plays the song before or after the current one, in the order the Music page lists the
        // playlist. Returns false when there isn't one (first or last song).
        private bool PlayRelative(int step)
        {
            if (_player is null || MusicPage.SharedLibVLC is not { } libVLC) return false;

            List<string> playlist = _service.GetMusicPaths(OmniService.CurrentUserId).Where(File.Exists).ToList();
            int index = playlist.FindIndex(p => string.Equals(p, CurrentPath() ?? _shownPath, StringComparison.OrdinalIgnoreCase));
            int target = index + step;
            if (index < 0 || target < 0 || target >= playlist.Count) return false;

            string path = playlist[target];
            _volumeToRestore = _player.Volume;
            // LibVLC won't play again from its Ended state until it has been stopped
            if (_player.State == VLCState.Ended)
                _player.Stop();
            using var media = new Media(libVLC, new Uri(path));
            _player.Play(media);

            // For Recently opened on Home
            _service.RecordLaunchByPath(OmniService.CurrentUserId, "Music", path);
            return true;
        }

        private void SeekSlider_MouseDown(object sender, MouseButtonEventArgs e) => _seeking = true;

        private void SeekSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _seeking = false;
            if (_player is not null && _player.IsSeekable)
                _player.Position = (float)SeekSlider.Value;
        }

        // ----- Favourite -----

        private const string HeartGlyph = "";
        private const string HeartFilledGlyph = "";
        private static readonly Brush FavouriteBrush = CreateFavouriteBrush();

        private static Brush CreateFavouriteBrush()
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
            brush.Freeze();
            return brush;
        }

        // Shows whether the song shown is a favourite; only songs in the playlist can be one
        private void UpdateFavourite()
        {
            if (Visibility != Visibility.Visible) return;

            int user = OmniService.CurrentUserId;
            int? id = _shownPath is null ? null : _service.GetItemId(user, "Music", _shownPath);
            bool favourite = id is not null && _service.IsFavourite(user, "Music", id.Value);

            FavouriteButton.IsEnabled = id is not null;
            FavouriteButton.Content = favourite ? HeartFilledGlyph : HeartGlyph;
            FavouriteButton.ToolTip = favourite ? "Remove from favourites" : "Add to favourites";
            if (favourite) FavouriteButton.Foreground = FavouriteBrush;
            else FavouriteButton.ClearValue(ForegroundProperty);
        }

        private void Favourite_Click(object sender, RoutedEventArgs e)
        {
            int user = OmniService.CurrentUserId;
            int? id = _shownPath is null ? null : _service.GetItemId(user, "Music", _shownPath);
            if (id is null) return;

            if (_service.IsFavourite(user, "Music", id.Value))
                _service.RemoveFavourite(user, "Music", id.Value);
            else
                _service.AddFavourite(user, "Music", id.Value);
            UpdateFavourite();
        }

        // ----- Volume -----

        private const string VolumeGlyph = "";
        private const string MuteGlyph = "";

        // Suppresses VolumeSlider_ValueChanged while the slider is set from the player
        private bool _syncingVolume;
        // When the popup last closed; a click on the button that closed it shouldn't reopen it
        private long _volumeClosedAt;

        private void Volume_Click(object sender, RoutedEventArgs e)
        {
            if (Environment.TickCount64 - _volumeClosedAt < 250) return;

            ShowVolume(_player?.Volume ?? 100);
            VolumePopup.IsOpen = true;
        }

        private void VolumePopup_Closed(object? sender, EventArgs e) => _volumeClosedAt = Environment.TickCount64;

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_syncingVolume) return;
            SetVolume((int)Math.Round(e.NewValue));
        }

        // Scroll up or down on the button or the bar to change the volume in steps of 5.
        // Counts from the bar, not the player: LibVLC applies a new volume a moment later,
        // so reading it straight back after a quick scroll would give the old value.
        private void Volume_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_player is null) return;
            SetVolume(Math.Clamp((int)VolumeSlider.Value + (e.Delta > 0 ? 5 : -5), 0, 100));
            e.Handled = true;
        }

        private void SetVolume(int volume)
        {
            if (_player is not null)
                _player.Volume = volume;
            ShowVolume(volume);
        }

        private void ShowVolume(int volume)
        {
            volume = Math.Clamp(volume, 0, 100);
            _syncingVolume = true;
            VolumeSlider.Value = volume;
            _syncingVolume = false;
            VolumeText.Text = volume.ToString();
            VolumeButton.Content = volume == 0 ? MuteGlyph : VolumeGlyph;
            VolumeButton.ToolTip = $"Volume {volume}";
        }

        private void Info_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow main)
                main.NavigateTo("Music");
        }
    }
}
