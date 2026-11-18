using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using Omni.Data;

namespace Omni.Pages
{
    // A row in the playlist
    public record PlaylistSong(string Title, string Details, string Path);

    public partial class MusicPage : UserControl
    {
        private readonly OmniService _service = new();

        // Files that Open file and Choose folder pick up
        private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".opus", ".wma",
        };

        private const string PlayGlyph = "";
        private const string PauseGlyph = "";

        private LibVLC _libVLC;
        private MediaPlayer? _mediaPlayer;

        // True while the user is dragging the seek bar, so playback doesn't move it
        private bool _seeking;

        // One player for the whole app. A new Music page is made each time it's opened, and the
        // music keeps playing after leaving it, so every page shares this player instead of
        // starting another one on top. Only the page on screen listens to its events.
        private static LibVLC? _sharedLibVLC;
        private static MediaPlayer? _sharedPlayer;
        private bool _listening;

        // For the sidebar's mini player; null until a Music page has been opened
        internal static MediaPlayer? SharedPlayer => _sharedPlayer;
        internal static LibVLC? SharedLibVLC => _sharedLibVLC;

        // The playlist is the current user's saved music; adding and removing songs updates the database
        private readonly ObservableCollection<PlaylistSong> _songs = new();


        public MusicPage()
        {
            InitializeComponent();

            Core.Initialize();

            _sharedLibVLC ??= new LibVLC();
            _sharedPlayer ??= new MediaPlayer(_sharedLibVLC);
            _libVLC = _sharedLibVLC;
            _mediaPlayer = _sharedPlayer;

            Loaded += (_, _) =>
            {
                Listen(true);
                ShowPlayerState();
            };
            Unloaded += (_, _) => Listen(false);

            PlaylistList.ItemsSource = _songs;
            // Already saved, so only show them; files that were moved or deleted are skipped
            AddSongs(_service.GetMusicPaths(OmniService.CurrentUserId).Where(File.Exists), save: false);
        }

        // Plays a song from outside the page (a card on Home). A page that was just opened isn't
        // on screen yet, so it waits until it is and listens to the player, to show the song.
        public void Play(string path)
        {
            if (IsLoaded)
            {
                PlaySong(path);
                return;
            }

            RoutedEventHandler? playOnce = null;
            playOnce = (_, _) =>
            {
                Loaded -= playOnce;
                PlaySong(path);
            };
            Loaded += playOnce;
        }

        // Stops the shared player; used when switching user, so the music doesn't carry on for the next one
        public static void StopAll() => _sharedPlayer?.Stop();

        // LibVLC raises these on its own thread
        private void Listen(bool on)
        {
            if (_mediaPlayer is null || on == _listening) return;
            _listening = on;

            if (on)
            {
                _mediaPlayer.Playing += OnPlaying;
                _mediaPlayer.Paused += OnPaused;
                _mediaPlayer.EndReached += OnEndReached;
                _mediaPlayer.Stopped += OnStopped;
                _mediaPlayer.TimeChanged += OnTimeChanged;
                _mediaPlayer.LengthChanged += OnLengthChanged;
            }
            else
            {
                _mediaPlayer.Playing -= OnPlaying;
                _mediaPlayer.Paused -= OnPaused;
                _mediaPlayer.EndReached -= OnEndReached;
                _mediaPlayer.Stopped -= OnStopped;
                _mediaPlayer.TimeChanged -= OnTimeChanged;
                _mediaPlayer.LengthChanged -= OnLengthChanged;
            }
        }

        // Coming back to the page while a song is playing or paused: show that song, not "Nothing playing"
        private void ShowPlayerState()
        {
            if (_mediaPlayer is null) return;

            if (_mediaPlayer.Volume >= 0)
                VolumeSlider.Value = _mediaPlayer.Volume;

            string? path = CurrentSongPath();
            bool active = _mediaPlayer.State is VLCState.Playing or VLCState.Paused or VLCState.Opening or VLCState.Buffering;
            if (path is null || !active) return;

            ShowTrackInfo();
            ShowPlayState(_mediaPlayer.IsPlaying);
            try
            {
                LoadAlbumArt(path);
            }
            catch (Exception ex) when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException or IOException)
            {
                AlbumArt.Source = null;
            }

            TotalTimeText.Text = FormatTime(_mediaPlayer.Length);
            CurrentTimeText.Text = FormatTime(_mediaPlayer.Time);
            SeekSlider.Value = _mediaPlayer.Position;
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
            UpdateFavouriteButton();
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
            UpdateFavouriteButton();
        }

        // ----- Favourite -----

        private const string HeartGlyph = "";
        private const string HeartFilledGlyph = "";
        private static readonly System.Windows.Media.Brush FavouriteBrush = CreateFavouriteBrush();

        private static System.Windows.Media.Brush CreateFavouriteBrush()
        {
            var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44));
            brush.Freeze();
            return brush;
        }

        // The song playing (or paused) now, or null once it has stopped
        private string? PlayingPath() =>
            _mediaPlayer?.State is VLCState.Playing or VLCState.Paused or VLCState.Opening or VLCState.Buffering
                ? CurrentSongPath()
                : null;

        // Shows whether the song playing now is a favourite. Only songs saved in the
        // playlist can be favourites, so the button is greyed out for anything else.
        private void UpdateFavouriteButton()
        {
            string? path = PlayingPath();
            int user = OmniService.CurrentUserId;
            int? id = path is null ? null : _service.GetItemId(user, "Music", path);
            bool favourite = id is not null && _service.IsFavourite(user, "Music", id.Value);

            FavouriteButton.IsEnabled = id is not null;
            FavouriteButton.Content = favourite ? HeartFilledGlyph : HeartGlyph;
            FavouriteButton.ToolTip = favourite ? "Remove from favourites" : "Add to favourites";
            if (favourite) FavouriteButton.Foreground = FavouriteBrush;
            else FavouriteButton.ClearValue(ForegroundProperty);
        }

        private void Favourite_Click(object sender, RoutedEventArgs e)
        {
            string? path = PlayingPath();
            int user = OmniService.CurrentUserId;
            int? id = path is null ? null : _service.GetItemId(user, "Music", path);
            if (id is null) return;

            if (_service.IsFavourite(user, "Music", id.Value))
                _service.RemoveFavourite(user, "Music", id.Value);
            else
                _service.AddFavourite(user, "Music", id.Value);
            UpdateFavouriteButton();
        }

        private string? CurrentSongPath()
        {
            using Media? media = _mediaPlayer?.Media;
            return media is null ? null : new Uri(media.Mrl).LocalPath;
        }

        // A song's tags, falling back to its file name (also used by the sidebar's mini player)
        internal static (string Title, string Details) ReadTrackInfo(string path)
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
        private void AddSongs(IEnumerable<string> paths, bool save = true) =>
            // Tags are only read for songs that aren't in the playlist yet
            AddSongs(paths.Where(path => IndexOfSong(path) < 0).Select(ToSong), save);

        private void AddSongs(IEnumerable<PlaylistSong> songs, bool save = true)
        {
            var added = new List<string>();
            foreach (var song in songs)
            {
                if (IndexOfSong(song.Path) >= 0) continue;

                _songs.Add(song);
                added.Add(song.Path);
            }
            if (save && added.Count > 0)
                _service.SaveScannedMusic(OmniService.CurrentUserId, added);
            PlaylistChanged();
        }

        private static PlaylistSong ToSong(string path)
        {
            var (title, details) = ReadTrackInfo(path);
            return new PlaylistSong(title, details, path);
        }

        // Open file: plays one song and adds it to the playlist
        private void OpenSong_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio Files|" + string.Join(";", AudioExtensions.Select(ext => "*" + ext))
            };
            if (dialog.ShowDialog() != true) return;

            AddSongs(new[] { dialog.FileName });
            PlaySong(dialog.FileName);
        }

        // Choose folder: adds every song in the folder and its subfolders to the playlist
        private async void ChooseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Choose a folder with music" };
            if (dialog.ShowDialog() != true) return;

            string folder = dialog.FolderName;
            StatusText.Text = "Scanning…";
            StatusText.Visibility = Visibility.Visible;
            EmptyText.Visibility = Visibility.Collapsed;

            List<PlaylistSong> found;
            try
            {
                found = await Task.Run(() => ScanFolder(folder));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                PlaylistChanged();
                StatusText.Text = "Couldn't read this folder.";
                StatusText.Visibility = Visibility.Visible;
                return;
            }

            AddSongs(found);
            if (found.Count == 0)
            {
                StatusText.Text = "No music found in this folder or its subfolders.";
                StatusText.Visibility = Visibility.Visible;
            }
        }

        // Finds songs in the folder and all of its subfolders. Runs off the UI thread,
        // tags included, so a big folder doesn't freeze the window.
        private static List<PlaylistSong> ScanFolder(string folder)
        {
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };

            return Directory.EnumerateFiles(folder, "*", options)
                .Where(path => AudioExtensions.Contains(Path.GetExtension(path)))
                .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase)
                .Select(ToSong)
                .ToList();
        }

        private void RemoveSong(PlaylistSong song)
        {
            _songs.Remove(song);
            _service.RemoveMusic(OmniService.CurrentUserId, song.Path);
            PlaylistChanged();
            // A song that's no longer in the playlist can't be a favourite
            UpdateFavouriteButton();
        }

        // Removes every song from the playlist and the database; the files stay on disk
        private void RemoveAll_Click(object sender, RoutedEventArgs e)
        {
            string songs = _songs.Count == 1 ? "1 song" : $"{_songs.Count} songs";
            var answer = MessageBox.Show(Window.GetWindow(this),
                $"Remove all {songs} from your playlist?\n\nThe files stay on your computer.",
                "Omni", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (answer != MessageBoxResult.Yes) return;

            _service.RemoveAllMusic(OmniService.CurrentUserId);
            _songs.Clear();
            PlaylistChanged();
            UpdateFavouriteButton();
        }

        private void PlaylistChanged()
        {
            EmptyText.Visibility = _songs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            StatusText.Text = _songs.Count == 1 ? "1 song" : $"{_songs.Count} songs";
            StatusText.Visibility = _songs.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            RemoveAllButton.Visibility = StatusText.Visibility;
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

            // For Recently opened on Home; songs not in the playlist aren't recorded
            _service.RecordLaunchByPath(OmniService.CurrentUserId, "Music", path);
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

        // Previous / Next: the song before or after the one playing now (or the selected one)
        private void Previous_Click(object sender, RoutedEventArgs e) => PlayRelative(-1);

        private void Next_Click(object sender, RoutedEventArgs e) => PlayRelative(1);

        private void PlayRelative(int step)
        {
            int index = IndexOfSong(CurrentSongPath() ?? (PlaylistList.SelectedItem as PlaylistSong)?.Path);
            int target = index + step;
            if (index < 0 || target < 0 || target >= _songs.Count) return;

            PlaySong(_songs[target].Path);
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
