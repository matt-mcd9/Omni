using System.Windows.Controls;
using System;
using System.Windows;
using LibVLCSharp.Shared;
using Microsoft.Win32;
using System.Collections;
using System.IO;
using Omni.Data;

namespace Omni.Pages
{
    public partial class VideosPage : UserControl
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        // Every video found by the last folder search (holds AppEntry objects).
        // Static so it survives leaving the page (MainWindow creates a new page
        // each time) and so the Home page can search and count it.
        public static ArrayList foundVideos = new ArrayList();
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

            videoView.MediaPlayer = _mediaPlayer;

            placeholder.Visibility = Visibility.Visible;

        }
        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter =
                    "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm|" +
                    "All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                using Media media = new Media(
                    _libVLC,
                    new Uri(dialog.FileName)
                );
                videoView.Visibility = Visibility.Visible;
                _mediaPlayer.Volume = 100;
                _mediaPlayer.Play(media);

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
            videoView.Visibility = Visibility.Collapsed;
            
            _mediaPlayer.Stop();
        }

        private void VideosPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.Stop();
            
        }

        private void VolumeSet(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _mediaPlayer.Volume = (int)e.NewValue;
            
        }

        // Lets the user pick a folder, then finds every video inside it (including subfolders)
        private void selectVideoFolder_Click(object sender, RoutedEventArgs e) {
            OpenFolderDialog dialog = new OpenFolderDialog();
            // Stop if the dialog was cancelled, so the current list is kept
            if (dialog.ShowDialog() != true) return;
            if (!Directory.Exists(dialog.FolderName)) return;

            // New search, so start the ArrayList over
            foundVideos.Clear();

            // Same video types the Open button's file filter accepts
            String[] videoTypes = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm" };

            foreach (String file in Directory.GetFiles(dialog.FolderName, "*", SearchOption.AllDirectories)) {
                // Only keep video files; ToLower so ".MP4" matches too
                if (videoTypes.Contains(Path.GetExtension(file).ToLower())) {
                    // Name is what the list shows, FullPath is what gets played
                    foundVideos.Add(new SoftwarePage.AppEntry {
                        Name = Path.GetFileNameWithoutExtension(file),
                        FullPath = file
                    });
                }
            }
            // Save what was found to the database (category 2 = Video) so the
            // Home page counts it; files already saved are skipped
            new OmniService().SaveLibraryItems(App.CurrentUser.UserId, 2, "Video", foundVideos);
            // Show the results, keeping whatever is already typed in the search box
            ShowVideos(searchVideos.Text);
        }

        // Filters the video list each time the text in the search box changes
        private void searchVideos_TextChanged(object sender, TextChangedEventArgs e) {
            ShowVideos(searchVideos.Text);
        }

        // Fills the ListBox with the videos in foundVideos whose name contains the search text
        private void ShowVideos(String filter) {
            listVideos.Items.Clear();

            foreach (SoftwarePage.AppEntry video in foundVideos) {
                // An empty search shows everything; otherwise match anywhere in the name, ignoring case
                if (String.IsNullOrWhiteSpace(filter) ||
                    video.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) {
                    listVideos.Items.Add(video);
                }
            }
        }

        // Double-click a video in the list to play it in the built-in player
        private void playVideo_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            // The list holds AppEntry objects; stop if nothing is selected
            if (listVideos.SelectedItem is not SoftwarePage.AppEntry video) return;

            // Same playback steps as Open_Click, using the selected video's path
            using Media media = new Media(_libVLC, new Uri(video.FullPath));
            videoView.Visibility = Visibility.Visible;
            _mediaPlayer.Volume = 100;
            _mediaPlayer.Play(media);
            // Save the play to LaunchHistory so it shows on the Home page
            new OmniService().RecordLaunchByPath(App.CurrentUser.UserId, video.FullPath);
        }

    }
}
