using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    // What a media card on the home page displays; Type is "Music", "Video", "Game" or "Software"
    public record MediaCardItem(string Title, string Subtitle, string Glyph, string FilePath, string Type);

    public partial class HomePage : UserControl
    {
        private readonly OmniService _service = new();

        // True while the current user has nothing saved in any library
        private bool _libraryEmpty;

        // How many cards Recently opened shows
        private const int RecentCount = 8;

        public HomePage()
        {
            InitializeComponent();

            string? username = _service.GetUser(OmniService.CurrentUserId)?.Username;
            GreetingText.Text = username is null ? "Welcome back" : $"Welcome back, {username}";
            // English like the rest of the UI, regardless of the Windows language
            DateText.Text = DateTime.Now.ToString("dddd, d MMMM", CultureInfo.GetCultureInfo("en-GB"));

            LoadLibraryCounts();

            // Like the library pages, files that are no longer on disk aren't shown
            int user = OmniService.CurrentUserId;
            // The whole history (at most 100), so Continue still finds a song or video
            // even when the last few things opened were all games or apps
            var recent = _service.GetRecentItems(user, int.MaxValue)
                .Where(item => File.Exists(item.FilePath))
                .ToList();
            ShowRecent(recent.Take(RecentCount).ToList(),
                recent.FirstOrDefault(item => item.Type is "Music" or "Video"));
            ShowFavourites(_service.GetFavourites(user)
                .Where(item => File.Exists(item.FilePath))
                .ToList());
        }

        private void LoadLibraryCounts()
        {
            // Only files that still exist are counted, as the library pages hide the rest
            int user = OmniService.CurrentUserId;
            int music = _service.GetMusicPaths(user).Count(File.Exists);
            int videos = _service.GetVideoPaths(user).Count(File.Exists);
            int games = _service.GetGamePaths(user).Count(File.Exists);
            int apps = _service.GetSoftwarePaths(user).Count(File.Exists);

            MusicCount.Text = music.ToString();
            VideoCount.Text = videos.ToString();
            GameCount.Text = games.ToString();
            SoftwareCount.Text = apps.ToString();

            _libraryEmpty = music + videos + games + apps == 0;
        }

        // Recently opened, most recent first. Continue only offers the last song or video, which
        // play on in Omni; games and apps are just launched again from their cards.
        private void ShowRecent(IList<OmniService.LibraryItem> items, OmniService.LibraryItem? resume)
        {
            var cards = items.Select(ToCard).ToList();
            RecentList.ItemsSource = cards;
            RecentEmpty.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Build your library is only for a user who hasn't added anything yet
            GetStartedCard.Visibility = cards.Count == 0 && _libraryEmpty ? Visibility.Visible : Visibility.Collapsed;

            if (resume is null)
            {
                ContinueCard.Visibility = Visibility.Collapsed;
                return;
            }

            var last = ToCard(resume);
            ContinueGlyph.Text = last.Glyph;
            ContinueTitle.Text = last.Title;
            ContinueSubtitle.Text = last.Subtitle;
            ContinueButton.Tag = last;
            ContinueCard.Visibility = Visibility.Visible;
        }

        private void ShowFavourites(IList<OmniService.LibraryItem> items)
        {
            var cards = items.Select(ToCard).ToList();
            FavouritesList.ItemsSource = cards;
            FavouritesEmpty.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static MediaCardItem ToCard(OmniService.LibraryItem item)
        {
            var (glyph, label) = item.Type  switch
            {
                "Music" => ("", "Music"),
                "Video" => ("", "Video"),
                "Game" => ("", "Game"),
                "Software" => ("", "App"),
                _ => ("", "File")
            };
            return new MediaCardItem(item.Title, label, glyph, item.FilePath, item.Type);
        }

        // Songs and videos open on their page and play in Omni; games and apps are launched
        private void MediaCard_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not MediaCardItem card) return;

            if (!File.Exists(card.FilePath))
            {
                MessageBox.Show($"Can't find \"{card.Title}\". It may have been moved or deleted.",
                    "Omni", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Window.GetWindow(this) is not MainWindow main) return;
            switch (card.Type)
            {
                case "Music":
                    main.NavigateTo("Music");
                    main.CurrentPage<MusicPage>()?.Play(card.FilePath);
                    break;
                case "Video":
                    main.NavigateTo("Videos");
                    main.CurrentPage<VideosPage>()?.Play(card.FilePath);
                    break;
                default:
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = card.FilePath, UseShellExecute = true });
                        _service.RecordLaunchByPath(OmniService.CurrentUserId, card.Type, card.FilePath);
                    }
                    catch (Win32Exception)
                    {
                        // e.g. the Windows permission prompt was cancelled
                        MessageBox.Show($"Couldn't open \"{card.Title}\".", "Omni", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    break;
            }
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string page && Window.GetWindow(this) is MainWindow main)
                main.NavigateTo(page);
        }
    }
}
