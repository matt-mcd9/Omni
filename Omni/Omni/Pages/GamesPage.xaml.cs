using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections;
using Omni.Data;

namespace Omni.Pages
{
    public partial class GamesPage : UserControl
    {

        // Full paths of the games currently shown in the ListBox, in the same order,
        // so the selected index in the list matches the path to launch
        List<String> hasGameExe = new List<String>();
        // Every .exe found in the selected folder
        String[] fileTypes;
        // Every game found by the last folder search (holds AppEntry objects).
        // Static so it survives leaving the page (MainWindow creates a new page
        // each time) and so the Home page can search and count it.
        public static ArrayList foundGames = new ArrayList();
        // Not used: the Videos page has its own foundVideos (VideosPage.foundVideos)
        public static ArrayList foundVideos = new ArrayList();
        public GamesPage()
        {
            InitializeComponent();
        }

        // Double-click a game in the list to launch it
        private void launchGame_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            int indexSelectedGame = listGames.SelectedIndex;

            ProcessStartInfo launchInfo = new ProcessStartInfo {
                FileName = hasGameExe[indexSelectedGame],
                UseShellExecute = true,
            };

            Process.Start(launchInfo);
            // Save the launch to LaunchHistory so it shows on the Home page
            new OmniService().RecordLaunchByPath(App.CurrentUser.UserId, hasGameExe[indexSelectedGame]);

        }

        // Lets the user pick a folder, then finds every .exe inside it (including subfolders)
        private void selectGameFolder_Click(object sender, EventArgs e) {

            // New search, so clear the results of the last one
            hasGameExe.Clear();
            listGames.Items.Clear();
            foundGames.Clear();


            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.ShowDialog();
            String folderName = dialog.FolderName;

            // FolderName is empty if the dialog was cancelled, so this also skips a cancel
            if (Directory.Exists(folderName)) {
                fileTypes = Directory.GetFiles(folderName, "*.exe", SearchOption.AllDirectories);
                foreach (String file in fileTypes) {
                    hasGameExe.Add(file);
                    // Also keep it in the ArrayList for searching and for the Home page
                    foundGames.Add(new SoftwarePage.AppEntry { Name = Path.GetFileNameWithoutExtension(file), FullPath = file });
                    // Show the name without the folder or extension
                    String results = System.IO.Path.GetFileNameWithoutExtension(file);
                    listGames.Items.Add(results);
                }
                // Save what was found to the database (category 3 = Game) so the
                // Home page counts it; files already saved are skipped
                new OmniService().SaveLibraryItems(App.CurrentUser.UserId, 3, "Game", foundGames);
            }
        }
        // Filters the games list as the user types
        private void searchGames_TextChanged(object sender, TextChangedEventArgs e) {
            // Rebuild hasGameExe along with the ListBox so launching by SelectedIndex still matches
            hasGameExe.Clear();
            listGames.Items.Clear();

            foreach (SoftwarePage.AppEntry game in foundGames) {
                // An empty search shows everything; otherwise match anywhere in the name, ignoring case
                if (String.IsNullOrWhiteSpace(searchGames.Text) ||
                    game.Name.Contains(searchGames.Text, StringComparison.OrdinalIgnoreCase)) {
                    hasGameExe.Add(game.FullPath);
                    listGames.Items.Add(game.Name);
                }
            }
        }


    }
}
