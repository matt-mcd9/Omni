using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Media.Imaging;

namespace Omni.Pages
{
    public partial class MusicPage : UserControl
    {

        private LibVLC _libVLC;
        private MediaPlayer? _mediaPlayer;
        private bool _isSeeking = false;


        public MusicPage()
        {
            InitializeComponent();

            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);

            _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
            _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;

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
        private void VolumeSet(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer == null)
                return;

            _mediaPlayer.Volume = (int)e.NewValue;
        }
        private void MediaPlayer_LengthChanged(
    object? sender,
    MediaPlayerLengthChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                SeekBar.Maximum = e.Length;

                TotalTimeText.Text = FormatTime(e.Length);
            });
        }

        private void MediaPlayer_TimeChanged(
            object? sender,
            MediaPlayerTimeChangedEventArgs e)
        {
            if (_isSeeking)
                return;

            Dispatcher.Invoke(() =>
            {
                SeekBar.Value = e.Time;

                CurrentTimeText.Text = FormatTime(e.Time);
            });
        }
        private void SeekBar_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isSeeking = true;
        }

        private void SeekBar_PreviewMouseLeftButtonUp(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Time = (long)SeekBar.Value;
            }

            _isSeeking = false;
        }
        private string FormatTime(long milliseconds)
        {
            TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);

            if (time.TotalHours >= 1)
            {
                return time.ToString(@"h\:mm\:ss");
            }

            return time.ToString(@"m\:ss");
        }

    }
}
