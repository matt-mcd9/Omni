using System.Windows.Controls;
using System;
using System.Windows;
using LibVLCSharp.Shared;
using Microsoft.Win32;


namespace Omni.Pages
{
    public partial class VideosPage : UserControl
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;

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

    }
}
