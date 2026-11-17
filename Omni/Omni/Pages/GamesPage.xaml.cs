using Microsoft.Win32;
using Omni.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Omni.Pages.SoftwarePage;

namespace Omni.Pages
{
    
    public partial class GamesPage : UserControl
    {
        String[] exeFileTypes;
        List<String> hasGameExe = new List<String>();
        String[] fileTypes;
        private readonly OmniService _service = new();

        public class GameDisplayItem {
            public string Name { get; set; } = "";
            public ImageSource? Icon { get; set; }
        }

        public GamesPage()
        {
            InitializeComponent();
            // adds file path tied to User 
            foreach (string path in _service.GetGamePaths(OmniService.CurrentUserId).Where(File.Exists))
            {
                hasGameExe.Add(path);
                listGames.Items.Add(new SoftwareDisplayItem {
                    Name = Path.GetFileNameWithoutExtension(path),
                    Icon = getFileIcon(path)
                });
            }
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
                    listGames.Items.Add(new GameDisplayItem {
                        Name = results,
                        Icon = getFileIcon(file)
                    });

                }
                scanExeFiles(folderName);
            }
            _service.SaveScannedGames(OmniService.CurrentUserId, hasGameExe);
        
        
        }


        private void scanExeFiles(string exePath, int depth = 0) {
            int maxDepth = 2;
            if (Directory.Exists(exePath)) {
                fileTypes = Directory.GetFiles(exePath, "*.exe", SearchOption.TopDirectoryOnly);

                foreach (String file in fileTypes) {
                    hasGameExe.Add(file);
                    String results = Path.GetFileNameWithoutExtension(file);
                    listGames.Items.Add(new GameDisplayItem {
                        Name = results,
                        Icon = getFileIcon(file)
                    });
                }

                if (depth < maxDepth) {
                    String[] subDirectory = Directory.GetDirectories(exePath);
                    foreach (String subDir in subDirectory) {
                        scanExeFiles(subDir, depth + 1);
                    }




                }
            }

        }

        private ImageSource? getFileIcon(string filePath) {
            using System.Drawing.Icon? icon =
                System.Drawing.Icon.ExtractAssociatedIcon(filePath);

            if (icon == null)
                return null;

            ImageSource image = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );

            image.Freeze();

            return image;
        }

    }
}
