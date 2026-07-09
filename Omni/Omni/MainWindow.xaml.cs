using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Omni.Pages;

namespace Omni
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            PageHost.Content = new HomePage();
        }

        // ----- Sidebar navigation: swap PageHost content based on which
        // RadioButton became checked ----- NIK was HEREEEEEEEEEEEEEEEgdfgdfgdfgdfgdfgdgggggg
        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (PageHost == null || sender is not RadioButton rb) return;

            PageHost.Content = rb.Name switch
            {
                nameof(NavHome) => new HomePage(),
                nameof(NavMusic) => new MusicPage(),
                nameof(NavVideos) => new VideosPage(),
                nameof(NavGames) => new GamesPage(),
                nameof(NavSoftware) => new SoftwarePage(),
                nameof(NavPlaylists) => new PlaylistsPage(),
                nameof(NavSettings) => new SettingsPage(),
                _ => PageHost.Content
            };
        }

        // ----- Custom title bar behaviour -----
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Maximize_Click(sender, e);
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
