using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Omni.Data;
using Omni.Models;

namespace Omni.Pages
{
    // What a media card on the home page displays
    public record MediaCardItem(string Title, string Subtitle, string Glyph, string FilePath);

    public partial class HomePage : UserControl
    {
        // Category ids seeded in OmniDbContext
        private const int MusicCategory = 1, VideoCategory = 2, GameCategory = 3, SoftwareCategory = 4;

        private readonly OmniService _service = new();

        public HomePage()
        {
            InitializeComponent();

            var now = DateTime.Now;
            GreetingText.Text = now.Hour switch
            {
                < 12 => "Good morning",
                < 18 => "Good afternoon",
                _ => "Good evening"
            };
            // English like the rest of the UI, regardless of the Windows language
            DateText.Text = now.ToString("dddd, d MMMM", CultureInfo.GetCultureInfo("en-GB"));

            LoadLibraryCounts();

            // Recent and favourites are stored per user; they stay empty until
            // the app has a current user to pass to GetRecentHistory / GetFavourites
            ShowRecent(new List<MediaItem>());
            ShowFavourites(new List<MediaItem>());        }

        private void LoadLibraryCounts()
        {
            var counts = _service.GetCategoryCounts();
            int count(int category) => counts.TryGetValue(category, out var n) ? n : 0;

            MusicCount.Text = count(MusicCategory).ToString();
            VideoCount.Text = count(VideoCategory).ToString();
            GameCount.Text = count(GameCategory).ToString();
            SoftwareCount.Text = count(SoftwareCategory).ToString();
        }

        // Most recent first; the first item also fills the Continue card
        private void ShowRecent(IList<MediaItem> items)
        {
            var cards = items.Select(ToCard).ToList();
            RecentList.ItemsSource = cards;
            RecentEmpty.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (cards.Count == 0)
            {
                ContinueCard.Visibility = Visibility.Collapsed;
                GetStartedCard.Visibility = Visibility.Visible;
                return;
            }

            var last = cards[0];
            ContinueGlyph.Text = last.Glyph;
            ContinueTitle.Text = last.Title;
            ContinueSubtitle.Text = last.Subtitle;
            ContinueButton.Tag = last;
            ContinueCard.Visibility = Visibility.Visible;
            GetStartedCard.Visibility = Visibility.Collapsed;
        }

        private void ShowFavourites(IList<MediaItem> items)
        {
            var cards = items.Select(ToCard).ToList();
            FavouritesList.ItemsSource = cards;
            FavouritesEmpty.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static MediaCardItem ToCard(MediaItem item)
        {
            var (glyph, label) = item.CategoryId switch
            {
                MusicCategory => ("", "Music"),
                VideoCategory => ("", "Video"),
                GameCategory => ("", "Game"),
                SoftwareCategory => ("", "App"),
                _ => ("", "File")
            };
            return new MediaCardItem(item.Title, label, glyph, item.FilePath);
        }

        private void MediaCard_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not MediaCardItem card) return;

            if (!File.Exists(card.FilePath))
            {
                MessageBox.Show($"Can't find \"{card.Title}\". It may have been moved or deleted.",
                    "Omni", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo { FileName = card.FilePath, UseShellExecute = true });
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string page && Window.GetWindow(this) is MainWindow main)
                main.NavigateTo(page);
        }
    }
}
