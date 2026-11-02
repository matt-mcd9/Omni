using System;
using System.Collections;
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
    // One row in the History list
    public record HistoryRow(string Title, string Subtitle, string When, string FilePath);
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

            // Recent and history are stored per user, so load them for the
            // default user that App.OnStartup set up. Favourites stay empty for now.
            // The last 50 launches, newest first
            var history = _service.GetRecentHistory(App.CurrentUser.UserId, 50);
            ShowHistory(history);

            // Recently opened: each item once (DistinctBy keeps its newest launch),
            // up to 10 items; the newest also fills the Continue card
            ShowRecent(history
                .Where(h => h.PlaylistItem is not null)
                .Select(h => h.PlaylistItem!)
                .DistinctBy(i => i.PlaylistItemId)
                .Take(10)
                .ToList());
            ShowFavourites(new List<PlaylistItem>());
            
        }
        // Fills the scrollable History list with every launch, newest first
        private void ShowHistory(IList<LaunchHistory> history)
        {
            // Turn each LaunchHistory row into the text the list shows:
            // title, type (Music/Video/Game/Software) and when it was opened
            var rows = history
                .Where(h => h.PlaylistItem is not null)
                .Select(h => new HistoryRow(
                    h.PlaylistItem!.Title,
                    h.PlaylistItem.MediaType,
                    h.LaunchedDate.ToString("d MMM, HH:mm", CultureInfo.GetCultureInfo("en-GB")),
                    h.PlaylistItem.FilePath))
                .ToList();

            HistoryList.ItemsSource = rows;
            // Show the "will appear here" message only when there's no history
            HistoryEmpty.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // Double-click a history row to open it again
        private void HistoryList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Stop if nothing is selected
            if (HistoryList.SelectedItem is not HistoryRow row) return;

            // The file may have been moved or deleted since it was last opened
            if (!File.Exists(row.FilePath))
            {
                MessageBox.Show($"Can't find \"{row.Title}\". It may have been moved or deleted.",
                    "Omni", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // UseShellExecute opens the file with its default Windows program
            Process.Start(new ProcessStartInfo { FileName = row.FilePath, UseShellExecute = true });
            // Opening it again counts as a new launch
            _service.RecordLaunchByPath(App.CurrentUser.UserId, row.FilePath);
        }
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
        private void ShowRecent(IList<PlaylistItem> items)
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

        private void ShowFavourites(IList<PlaylistItem> items)
        {
            var cards = items.Select(ToCard).ToList();
            FavouritesList.ItemsSource = cards;
            FavouritesEmpty.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static MediaCardItem ToCard(PlaylistItem item)
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
            // Save the launch so it shows in Recently opened and History
            _service.RecordLaunchByPath(App.CurrentUser.UserId, card.FilePath);
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string page && Window.GetWindow(this) is MainWindow main)
                main.NavigateTo(page);
        }
        
        // Searches the ArrayLists filled by the Music, Videos, Games and Software pages
        private void HomeSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filter = HomeSearchBox.Text.Trim();
            // Hide the results section when the search box is empty
            if (filter.Length == 0)
            {
                SearchResultsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // Collect matches from each page's ArrayList. The \uE... codes are the
            // same Segoe icons the library tiles use (music note, video, game, apps).
            var results = new List<MediaCardItem>();
            AddMatches(results, MusicPage.foundSongs, filter, "\uE8D6", "Music");
            AddMatches(results, VideosPage.foundVideos, filter, "\uE714", "Video");
            AddMatches(results, GamesPage.foundGames, filter, "\uE7FC", "Game");
            AddMatches(results, SoftwarePage.foundApps, filter, "\uE71D", "App");

            // Show the matches as media cards; clicking one runs MediaCard_Click
            SearchResultsList.ItemsSource = results;
            // Show "Nothing matches" only when there are no results
            SearchEmpty.Visibility = results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            SearchResultsPanel.Visibility = Visibility.Visible;
        }

        // Adds a card to results for every entry in one page's ArrayList whose
        // name contains the search text. glyph and label say which page it came from.
        private static void AddMatches(List<MediaCardItem> results, ArrayList found, string filter, string glyph, string label)
        {
            foreach (SoftwarePage.AppEntry entry in found)
            {
                // Match anywhere in the name, ignoring upper/lower case
                if (entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    results.Add(new MediaCardItem(entry.Name, label, glyph, entry.FullPath));
            }
        }
    }
}
