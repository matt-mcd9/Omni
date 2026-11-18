using Microsoft.Win32;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Path = System.IO.Path;
using Omni.Data;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace Omni.Pages
{
    public partial class SoftwarePage : UserControl
    {
        private readonly OmniService _service = new();

        String[] exeFileTypes;
        String[] fileTypes;
        List<String> hasExeFiles = new List<String>();

        public class SoftwareDisplayItem {
            public string Name { get; set; } = "";
            public ImageSource? Icon { get; set; }
        }

        public SoftwarePage()
        {
            InitializeComponent();
            ShowSavedApps();
        }

        // Lists every app saved for the current user, skipping files that were moved or deleted.
        // hasExeFiles is rebuilt alongside so the list index still finds the path to launch.
        private void ShowSavedApps() {
            hasExeFiles.Clear();
            listApplications.Items.Clear();

            foreach (string path in _service.GetSoftwarePaths(OmniService.CurrentUserId).Where(File.Exists)) {
                hasExeFiles.Add(path);
                listApplications.Items.Add(new SoftwareDisplayItem {
                    Name = Path.GetFileNameWithoutExtension(path),
                    Icon = getFileIcon(path)
                });
            }
        }

        //click event for launching (find item, match to full path, double click to launch)
        private void launchApp_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            int indexSelectedApp = listApplications.SelectedIndex;

            ProcessStartInfo launchInfo = new ProcessStartInfo {
                FileName = hasExeFiles[indexSelectedApp],
                UseShellExecute = true                
            };

            Process.Start(launchInfo);
            // For Recently opened on Home
            _service.RecordLaunchByPath(OmniService.CurrentUserId, "Software", hasExeFiles[indexSelectedApp]);


        }

        //Add exception handling for no directory?
        private void selectFolder_Click(object sender, EventArgs e) {
            hasExeFiles.Clear();
            listApplications.Items.Clear();

            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.ShowDialog();
            String folderName = dialog.FolderName;

            if (Directory.Exists(folderName)) {

                fileTypes = Directory.GetFiles(folderName, "*.lnk", SearchOption.AllDirectories);

                foreach (String file in fileTypes) {
                        hasExeFiles.Add(file);
                        String results = System.IO.Path.GetFileNameWithoutExtension(file);
                        listApplications.Items.Add(new SoftwareDisplayItem {
                            Name = results,
                            Icon = getFileIcon(file)
                        });
                }
                scanExeFiles(folderName);

            }
            _service.SaveScannedSoftware(OmniService.CurrentUserId, hasExeFiles);
            // Shows the new apps together with the ones saved before (and restores the list if the dialog was cancelled)
            ShowSavedApps();

        }

        //Separate method to stop from scanning too far into subdirectories 
        private void scanExeFiles(string exePath, int depth = 0) {
            int maxDepth = 2;
            if (Directory.Exists(exePath)) { 
                exeFileTypes = Directory.GetFiles(exePath, "*.exe", SearchOption.TopDirectoryOnly);

                foreach (String file in exeFileTypes) {
                    hasExeFiles.Add(file);

                    String results = Path.GetFileNameWithoutExtension(file);

                    listApplications.Items.Add(new SoftwareDisplayItem {
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
