using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using Omni.Data;
using Path = System.IO.Path;

namespace Omni.Pages
{
    public partial class SoftwarePage : UserControl
    {
        // Every file found in the selected folder (all types, filtered below)
        String[] fileTypes;
        // Full paths of the apps currently shown in the ListBox, in the same order,
        // so the selected index in the list matches the path to launch
        List<String> hasExeFiles = new List<String>();
        // Every app found by the last folder search (holds AppEntry objects).
        // Static so it survives leaving the page (MainWindow creates a new page
        // each time) and so the Home page can search and count it.
        public static ArrayList foundApps = new ArrayList();
        public SoftwarePage()
        {
            InitializeComponent();
        }

        //click event for launching (find item, match to full path, double click to launch)
        private void launchApp_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            int indexSelectedApp = listApplications.SelectedIndex;

            // UseShellExecute lets Windows open .lnk shortcuts as well as .exe files
            ProcessStartInfo launchInfo = new ProcessStartInfo {
                FileName = hasExeFiles[indexSelectedApp],
                UseShellExecute = true
            };

            Process.Start(launchInfo);
            // Save the launch to LaunchHistory so it shows on the Home page
            new OmniService().RecordLaunchByPath(App.CurrentUser.UserId, hasExeFiles[indexSelectedApp]);


        }

        // Lets the user pick a folder, then finds every .exe and .lnk inside it (including subfolders)
        private void selectFolder_Click(object sender, EventArgs e)
        {
            // New search, so clear the results of the last one
            hasExeFiles.Clear();
            listApplications.Items.Clear();
            foundApps.Clear();
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.ShowDialog();
            String folderName = dialog.FolderName;

            // FolderName is empty if the dialog was cancelled, so this also skips a cancel
            if (Directory.Exists(folderName))
            {

                fileTypes = Directory.GetFiles(folderName, "*", SearchOption.AllDirectories);

                foreach (String file in fileTypes)
                {

                    // Only keep programs and shortcuts
                    String fileType = Path.GetExtension(file);
                    if (fileType == ".lnk" || fileType == ".exe")
                    {
                        hasExeFiles.Add(file);
                        // Show the name without the folder or extension, e.g. "notepad"
                        String results = System.IO.Path.GetFileNameWithoutExtension(file);
                        listApplications.Items.Add(results);
                        // Also keep it in the ArrayList for searching and for the Home page
                        foundApps.Add(new AppEntry { Name = results, FullPath = file });
                    }
                }
                // Save what was found to the database (category 4 = Software) so the
                // Home page counts it; files already saved are skipped
                new OmniService().SaveLibraryItems(App.CurrentUser.UserId, 4, "Software", foundApps);

            }
        }

        // Filters the list each time the text in the search box changes
        private void searchBox_TextChanged(object sender, TextChangedEventArgs e) {
                ShowApps(searchBox.Text);
        }

            // Fills the ListBox with the apps in foundApps whose name contains the search text
            private void ShowApps(String filter) {
                // Rebuild hasExeFiles along with the ListBox so launching by SelectedIndex still matches
                hasExeFiles.Clear();
                listApplications.Items.Clear();

                foreach (AppEntry app in foundApps) {
                    // An empty search shows everything; otherwise match anywhere in the name, ignoring case
                    if (String.IsNullOrWhiteSpace(filter) ||
                        app.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) {
                        hasExeFiles.Add(app.FullPath);
                        listApplications.Items.Add(app); // ToString() shows Name
                    }
                }
            }

        // One file found by a folder search: the name to show and the path to open.
        // The Music, Videos and Games pages reuse this class too.
        public class AppEntry {
            public String Name { get; set; } = string.Empty;
            public String FullPath { get; set; } = string.Empty;
            // ListBoxes call ToString() to decide what text to show
            public override string ToString() => Name;
        }
    }
}
