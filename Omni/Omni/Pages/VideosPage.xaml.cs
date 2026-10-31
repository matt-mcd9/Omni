using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LibVLCSharp.Shared;
using Microsoft.Win32;

namespace Omni.Pages
{
    // A playable file found by scanning a folder
    public record VideoFile(string Name, string Path, string Details);

    public partial class VideosPage : UserControl
    {
        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".m4v",
            ".flv", ".mpg", ".mpeg", ".ts", ".m2ts", ".3gp",
        };

        private const string PlayGlyph = "";
        private const string PauseGlyph = "";

        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;

        private string? _currentPath;
        // File picked before the player finished initialising
        private string? _pendingPath;
        // True while the user is dragging the seek bar, so playback doesn't move it
        private bool _seeking;

        public VideosPage()
        {
            InitializeComponent();

            Loaded += VideosPage_Loaded;
        }

        private async void VideosPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= VideosPage_Loaded;

            await InitializePlayerAsync();
        }

        private async Task InitializePlayerAsync()
        {
            await Task.Run(() =>
            {
                Core.Initialize();

                _libVLC = new LibVLC();
                _mediaPlayer = new MediaPlayer(_libVLC);
            });

            // LibVLC raises these on its own thread
            _mediaPlayer!.Playing += OnPlaying;
            _mediaPlayer.Paused += OnPausedOrStopped;
            _mediaPlayer.Stopped += OnPausedOrStopped;
            _mediaPlayer.EndReached += OnEndReached;
            _mediaPlayer.TimeChanged += OnTimeChanged;
            _mediaPlayer.LengthChanged += OnLengthChanged;

            videoView.MediaPlayer = _mediaPlayer;
            placeholder.Visibility = Visibility.Visible;

            if (_pendingPath is not null)
            {
                PlayFile(_pendingPath);
                _pendingPath = null;
            }
        }

        // ----- Folder scanning -----

        private async void ChooseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Choose a folder with videos" };
            if (dialog.ShowDialog() != true) return;

            string folder = dialog.FolderName;
            FolderText.Text = folder;
            FolderText.ToolTip = folder;
            FolderText.Visibility = Visibility.Visible;
            StatusText.Text = "Scanning…";
            StatusText.Visibility = Visibility.Visible;
            EmptyText.Visibility = Visibility.Collapsed;
            VideoList.ItemsSource = null;

            List<VideoFile> videos;
            try
            {
                videos = await Task.Run(() => ScanFolder(folder));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                StatusText.Text = "Couldn't read this folder.";
                return;
            }

            VideoList.ItemsSource = videos;
            StatusText.Text = videos.Count == 1 ? "1 video" : $"{videos.Count} videos";
            if (videos.Count == 0)
            {
                EmptyText.Text = "No videos found in this folder or its subfolders.";
                EmptyText.Visibility = Visibility.Visible;
            }
        }

        // Finds playable videos in the folder and all of its subfolders
        private static List<VideoFile> ScanFolder(string folder)
        {
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };

            return Directory.EnumerateFiles(folder, "*", options)
                .Where(path => VideoExtensions.Contains(Path.GetExtension(path)))
                .Select(path => ToVideoFile(folder, path))
                .OrderBy(v => v.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static VideoFile ToVideoFile(string root, string path)
        {
            var info = new FileInfo(path);
            var details = new List<string>
            {
                info.Extension.TrimStart('.').ToUpperInvariant(),
                FormatSize(info.Length),
            };

            // Show which subfolder the file came from
            string relativeDir = Path.GetRelativePath(root, info.DirectoryName ?? root);
            if (relativeDir != ".") details.Add(relativeDir);

            return new VideoFile(Path.GetFileNameWithoutExtension(path), path, string.Join(" · ", details));
        }

        private static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return unit == 0 ? $"{bytes} B" : $"{size:0.#} {units[unit]}";
        }

        private void VideoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideoList.SelectedItem is VideoFile video)
                PlayFile(video.Path);
        }

        // ----- Playback -----

        private void PlayFile(string path)
        {
            if (_libVLC is null || _mediaPlayer is null)
            {
                _pendingPath = path;
                return;
            }

            _currentPath = path;
            NowPlayingText.Text = Path.GetFileNameWithoutExtension(path);

            using Media media = new Media(_libVLC, new Uri(path));
            placeholder.Visibility = Visibility.Collapsed;
            videoView.Visibility = Visibility.Visible;
            _mediaPlayer.Volume = (int)VolumeSlider.Value;
            _mediaPlayer.Play(media);
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter =
                    "Video Files|" + string.Join(";", VideoExtensions.Select(ext => "*" + ext)) + "|" +
                    "All Files|*.*"
            };

            if (dialog.ShowDialog() != true) return;

            // If the file is in the scanned list, select it so the list stays in sync
            var match = (VideoList.ItemsSource as IEnumerable<VideoFile>)?
                .FirstOrDefault(v => string.Equals(v.Path, dialog.FileName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                VideoList.SelectedItem = match;
                VideoList.ScrollIntoView(match);
            }
            else
            {
                VideoList.SelectedItem = null;
                PlayFile(dialog.FileName);
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer is null) return;

            if (_mediaPlayer.IsPlaying)
                _mediaPlayer.SetPause(true);
            else if (_mediaPlayer.State == VLCState.Paused)
                _mediaPlayer.SetPause(false);
            else if (_currentPath is not null)
                PlayFile(_currentPath);
            else if (VideoList.Items.Count > 0)
                VideoList.SelectedIndex = 0;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer?.Stop();
            ResetPlayerUi();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (VideoList.SelectedIndex > 0)
                VideoList.SelectedIndex--;
        }

        private void Next_Click(object sender, RoutedEventArgs e) => PlayNext();

        private bool PlayNext()
        {
            if (VideoList.SelectedIndex < 0 || VideoList.SelectedIndex >= VideoList.Items.Count - 1)
                return false;

            VideoList.SelectedIndex++;
            VideoList.ScrollIntoView(VideoList.SelectedItem);
            return true;
        }

        private void ResetPlayerUi()
        {
            videoView.Visibility = Visibility.Collapsed;
            placeholder.Visibility = Visibility.Visible;
            SeekSlider.Value = 0;
            CurrentTimeText.Text = FormatTime(0);
            PlayPauseButton.Content = PlayGlyph;
            PlayPauseButton.ToolTip = "Play";
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

        // ----- Player events (raised on LibVLC's thread, so marshal to the UI) -----

        private void OnPlaying(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() =>
        {
            PlayPauseButton.Content = PauseGlyph;
            PlayPauseButton.ToolTip = "Pause";
        });

        private void OnPausedOrStopped(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() =>
        {
            PlayPauseButton.Content = PlayGlyph;
            PlayPauseButton.ToolTip = "Play";
        });

        // Continue with the next video in the list; otherwise go back to idle
        private void OnEndReached(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() =>
        {
            if (!PlayNext())
                ResetPlayerUi();
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

        private static string FormatTime(long milliseconds)
        {
            var t = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
            return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
        }

        private void VideosPage_Unloaded(object sender, RoutedEventArgs e)
        {
            var player = _mediaPlayer;
            var libVLC = _libVLC;
            if (player is null) return;

            player.Playing -= OnPlaying;
            player.Paused -= OnPausedOrStopped;
            player.Stopped -= OnPausedOrStopped;
            player.EndReached -= OnEndReached;
            player.TimeChanged -= OnTimeChanged;
            player.LengthChanged -= OnLengthChanged;

            videoView.MediaPlayer = null;
            _mediaPlayer = null;
            _libVLC = null;

            // Stopping LibVLC can block briefly, so release it off the UI thread
            Task.Run(() =>
            {
                player.Stop();
                player.Dispose();
                libVLC?.Dispose();
            });
        }
    }
}
