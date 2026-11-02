using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;

namespace Omni.Pages
{
    public partial class GamesPage : UserControl
    {
        String[] exeFileTypes;
        List<String> hasGameExe = new List<String>();
        String[] fileTypes;
        public GamesPage()
        {
            InitializeComponent();
        }

        private void launchGame_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            int indexSelectedGame = listGames.SelectedIndex;

            ProcessStartInfo launchInfo = new ProcessStartInfo {
                FileName = hasGameExe[indexSelectedGame],
                UseShellExecute = true,
            };

            Process.Start(launchInfo);

        }

        private void selectGameFolder_Click(object sender, EventArgs e) {

            hasGameExe.Clear();
            listGames.Items.Clear();


            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.ShowDialog();
            String folderName = dialog.FolderName;

            if (Directory.Exists(folderName)) {
                fileTypes = Directory.GetFiles(folderName, "*.lnk", SearchOption.AllDirectories);


                foreach (String file in fileTypes) {
                    Debug.WriteLine(file);
                    hasGameExe.Add(file);
                    String results = System.IO.Path.GetFileNameWithoutExtension(file);
                    listGames.Items.Add(results);

                }
                scanExeFiles(folderName);
            }
        
        
        }


        private void scanExeFiles(string exePath, int depth = 0) {
            int maxDepth = 2;
            if (Directory.Exists(exePath)) {
                fileTypes = Directory.GetFiles(exePath, "*.exe", SearchOption.TopDirectoryOnly);

                foreach (String file in fileTypes) {
                    hasGameExe.Add(file);
                    String results = Path.GetFileNameWithoutExtension(file);
                    listGames.Items.Add(results);
                }

                if (depth < maxDepth) {
                    String[] subDirectory = Directory.GetDirectories(exePath);
                    foreach (String subDir in subDirectory) {
                        scanExeFiles(subDir, depth + 1);
                    }




                }
            }

        }



    }
}
