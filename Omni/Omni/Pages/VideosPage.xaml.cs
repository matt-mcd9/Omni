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
        private bool _isSeeking = false;
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
                _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
                _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;

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
        private void SeekBar_PreviewMouseLeftButtonDown(object sender,System.Windows.Input.MouseButtonEventArgs e)
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
