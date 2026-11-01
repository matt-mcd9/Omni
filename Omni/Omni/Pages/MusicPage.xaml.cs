using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Media.Imaging;
using System.Collections;
using Omni.Data;

namespace Omni.Pages
{
    public partial class MusicPage : UserControl
    {

        private LibVLC _libVLC;
        private MediaPlayer? _mediaPlayer;
        // Every song found by the last folder search (holds AppEntry objects).
        // Static so it survives leaving the page (MainWindow creates a new page
        // each time) and so the Home page can search and count it.
        public static ArrayList foundSongs = new ArrayList();

        public MusicPage()
        {
            InitializeComponent();

            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
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

        // Lets the user pick a folder, then finds every song inside it (including subfolders)
        private void selectMusicFolder_Click(object sender, RoutedEventArgs e) {
            OpenFolderDialog dialog = new OpenFolderDialog();
            // Stop if the dialog was cancelled, so the current list is kept
            if (dialog.ShowDialog() != true) return;
            if (!Directory.Exists(dialog.FolderName)) return;

            // New search, so start the ArrayList over
            foundSongs.Clear();

            foreach (String file in Directory.GetFiles(dialog.FolderName, "*", SearchOption.AllDirectories)) {
                // Only keep audio files; ToLower so ".MP3" matches too
                String fileType = Path.GetExtension(file).ToLower();
                if (fileType == ".mp3" || fileType == ".wav" || fileType == ".flac" || fileType == ".m4a") {
                    // Name is what the list shows, FullPath is what gets played
                    foundSongs.Add(new SoftwarePage.AppEntry {
                        Name = Path.GetFileNameWithoutExtension(file),
                        FullPath = file
                    });
                }
            }
            // Save what was found to the database (category 1 = Music) so the
            // Home page counts it; files already saved are skipped
            new OmniService().SaveLibraryItems(App.CurrentUser.UserId, 1, "Music", foundSongs);
            // Show the results, keeping whatever is already typed in the search box
            ShowSongs(searchMusic.Text);
        }

        // Filters the song list each time the text in the search box changes
        private void searchMusic_TextChanged(object sender, TextChangedEventArgs e) {
            ShowSongs(searchMusic.Text);
        }

        // Fills the ListBox with the songs in foundSongs whose name contains the search text
        private void ShowSongs(String filter) {
            listSongs.Items.Clear();

            foreach (SoftwarePage.AppEntry song in foundSongs) {
                // An empty search shows everything; otherwise match anywhere in the name, ignoring case
                if (String.IsNullOrWhiteSpace(filter) ||
                    song.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) {
                    listSongs.Items.Add(song);
                }
            }
        }

        // Double-click a song in the list to play it in the built-in player
        private void playSong_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            // The list holds AppEntry objects; stop if nothing is selected
            if (listSongs.SelectedItem is not SoftwarePage.AppEntry song) return;

            LoadAlbumArt(song.FullPath);

            // Same playback steps as Open_Click, using the selected song's path
            using Media media = new Media(_libVLC, new Uri(song.FullPath));
            _mediaPlayer.Volume = 100;
            _mediaPlayer.Play(media);
            // Save the play to LaunchHistory so it shows on the Home page
            new OmniService().RecordLaunchByPath(App.CurrentUser.UserId, song.FullPath);
        }
 
    }
}
