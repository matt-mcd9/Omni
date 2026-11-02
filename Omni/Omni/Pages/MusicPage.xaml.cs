using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace Omni.Pages
{
    // A row in the playlist
    public record PlaylistSong(string Title, string Details, string Path);

    public partial class MusicPage : UserControl
    {

        private const string PlayGlyph = "";
        private const string PauseGlyph = "";

        private LibVLC _libVLC;
        private MediaPlayer? _mediaPlayer;

        // True while the user is dragging the seek bar, so playback doesn't move it
        private bool _seeking;

        // Placeholder storage: the playlist only lasts while the app is running.
        // Replace LoadPlaylistPaths and SavePlaylistPaths once it's decided where it is saved.
        private static List<string> _storedPlaylistPaths = new();

        private readonly ObservableCollection<PlaylistSong> _songs = new();


        public MusicPage()
        {
            InitializeComponent();

            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);

            // LibVLC raises these on its own thread
            _mediaPlayer.Playing += OnPlaying;
            _mediaPlayer.Paused += OnPaused;
            _mediaPlayer.EndReached += OnEndReached;
            _mediaPlayer.Stopped += OnStopped;
            _mediaPlayer.TimeChanged += OnTimeChanged;
            _mediaPlayer.LengthChanged += OnLengthChanged;

            PlaylistList.ItemsSource = _songs;
            AddSongs(LoadPlaylistPaths());
        }

        // ----- Player events (marshalled to the UI thread) -----

        private void OnPlaying(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() =>
        {
            ShowPlayState(true);
            ShowTrackInfo();

            // Opening a song resets the player's volume, so reapply the slider
            if (_mediaPlayer is not null)
                _mediaPlayer.Volume = (int)VolumeSlider.Value;
        });

        private void OnPaused(object? sender, EventArgs e) =>
            Dispatcher.BeginInvoke(() => ShowPlayState(false));

        // Continue with the next song, unless the user has left this page
        private void OnEndReached(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() =>
        {
            ShowPlayState(false);

            int index = IndexOfSong(CurrentSongPath());
            if (IsLoaded && index >= 0 && index < _songs.Count - 1)
                PlaySong(_songs[index + 1].Path);
        });

        private void OnStopped(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() =>
        {
            // Starting a song also raises this, and it can arrive once the new song is playing
            if (_mediaPlayer?.IsPlaying == true) return;

            ShowPlayState(false);
            TitleText.Text = "Nothing playing";
            DetailsText.Visibility = Visibility.Collapsed;
            SeekSlider.Value = 0;
            CurrentTimeText.Text = FormatTime(0);
            TotalTimeText.Text = FormatTime(0);
        });

        private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            long time = e.Time;
            Dispatcher.BeginInvoke(() =>
            {
                if (_mediaPlayer is null) return;

                CurrentTimeText.Text = FormatTime(time);
                if (!_seeking)
                    SeekSlider.Value = _mediaPlayer.Position;
            });
        }

        private void OnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
        {
            long length = e.Length;
            Dispatcher.BeginInvoke(() => TotalTimeText.Text = FormatTime(length));
        }

        private void ShowPlayState(bool playing)
        {
            PlayPauseButton.Content = playing ? PauseGlyph : PlayGlyph;
            PlayPauseButton.ToolTip = playing ? "Pause" : "Play";
        }

        // Shows the playing song's details and highlights its row in the playlist
        private void ShowTrackInfo()
        {
            string? path = CurrentSongPath();
            if (path is null) return;

            var (title, details) = ReadTrackInfo(path);
            TitleText.Text = title;
            DetailsText.Text = details;
            DetailsText.Visibility = details.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

            int index = IndexOfSong(path);
            PlaylistList.SelectedItem = index >= 0 ? _songs[index] : null;
        }

        private string? CurrentSongPath()
        {
            using Media? media = _mediaPlayer?.Media;
            return media is null ? null : new Uri(media.Mrl).LocalPath;
        }

        // A song's tags, falling back to its file name
        private static (string Title, string Details) ReadTrackInfo(string path)
        {
            string title = Path.GetFileNameWithoutExtension(path);
            string details = "";

            try
            {
                using var file = TagLib.File.Create(path);
                if (!string.IsNullOrWhiteSpace(file.Tag.Title)) title = file.Tag.Title;
                details = string.Join(" · ", new[] { file.Tag.JoinedPerformers, file.Tag.Album }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));
            }
            catch (Exception ex) when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException or IOException)
            {
                // Unreadable tags: the file name is still enough to show
            }

            return (title, details);
        }

        // ----- Playlist -----

        private static List<string> LoadPlaylistPaths() => new(_storedPlaylistPaths);

        private static void SavePlaylistPaths(IEnumerable<string> paths) => _storedPlaylistPaths = paths.ToList();

        private int IndexOfSong(string? path)
        {
            for (int i = 0; i < _songs.Count; i++)
            {
                if (string.Equals(_songs[i].Path, path, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        // Adds songs to the end of the playlist, skipping any already in it
        private void AddSongs(IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                if (IndexOfSong(path) >= 0) continue;

                var (title, details) = ReadTrackInfo(path);
                _songs.Add(new PlaylistSong(title, details, path));
            }
            PlaylistChanged();
        }

        private void RemoveSong(PlaylistSong song)
        {
            _songs.Remove(song);
            PlaylistChanged();
        }

        private void PlaylistChanged()
        {
            SavePlaylistPaths(_songs.Select(song => song.Path));

            EmptyText.Visibility = _songs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            StatusText.Text = _songs.Count == 1 ? "1 song" : $"{_songs.Count} songs";
            StatusText.Visibility = _songs.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void PlaySong(string path)
        {
            if (!File.Exists(path))
            {
                StatusText.Text = "Can't find this song. It may have been moved or deleted.";
                return;
            }

            try
            {
                LoadAlbumArt(path);
            }
            catch (Exception ex) when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException or IOException)
            {
                AlbumArt.Source = null;
            }

            // LibVLC won't play again from its Ended state until it has been stopped
            if (_mediaPlayer?.State == VLCState.Ended)
                _mediaPlayer.Stop();

            using Media media = new Media(_libVLC, new Uri(path));
            _mediaPlayer?.Play(media);
        }

        private void AddSongs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Audio Files|*.mp3", Multiselect = true };
            if (dialog.ShowDialog() == true)
                AddSongs(dialog.FileNames);
        }

        private void Song_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is PlaylistSong song)
                PlaySong(song.Path);
        }

        private void RemoveSong_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is PlaylistSong song)
                RemoveSong(song);
        }

        private void PlaylistList_KeyDown(object sender, KeyEventArgs e)
        {
            if (PlaylistList.SelectedItem is not PlaylistSong song) return;

            if (e.Key == Key.Enter) PlaySong(song.Path);
            else if (e.Key == Key.Delete) RemoveSong(song);
        }

        private static string FormatTime(long milliseconds)
        {
            var t = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
            return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
        }

        // ----- Controls -----

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            // LibVLC won't play again from its Ended state until it has been stopped
            if (_mediaPlayer?.State == VLCState.Ended)
                _mediaPlayer.Stop();

            // Pause only toggles a song that is loaded and running
            if (_mediaPlayer?.State == VLCState.Stopped)
                Play_Click(sender, e);
            else
                Pause_Click(sender, e);
        }

        private void VolumeSet(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer is not null)
                _mediaPlayer.Volume = (int)e.NewValue;
        }

        private void SeekSlider_MouseDown(object sender, MouseButtonEventArgs e) => _seeking = true;

        private void SeekSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _seeking = false;
            if (_mediaPlayer is not null && _mediaPlayer.IsSeekable)
                _mediaPlayer.Position = (float)SeekSlider.Value;
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter =
                    "Audio Files|*.mp3;"
            };



            if (dialog.ShowDialog() == true)
            {
                LoadAlbumArt(dialog.FileName);


                using Media media = new Media(_libVLC,new Uri(dialog.FileName));
                _mediaPlayer.Volume = 100;
                _mediaPlayer.Play(media);

            }
        }

        private void LoadAlbumArt(string filePath)
        {
            var file = TagLib.File.Create(filePath);

            if (file.Tag.Pictures.Length == 1 )
            {
                var picture = file.Tag.Pictures[0];

                using MemoryStream stream = new MemoryStream(picture.Data.Data);

                BitmapImage image = new BitmapImage();

                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();

                AlbumArt.Source = image;
            }
            else
            {
                AlbumArt.Source = new BitmapImage(
                    new Uri("/Pictures/album.png", UriKind.Relative));
            }
        }
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.Play();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.Pause();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {

            _mediaPlayer.Stop();
            AlbumArt.Source = null;

        }

 
    }
}
