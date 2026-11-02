using Microsoft.Win32;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace Omni.Pages
{
    public partial class SoftwarePage : UserControl
    {

        String[] exeFileTypes;
        String[] fileTypes;
        List<String> hasExeFiles = new List<String>();
        public SoftwarePage()
        {
            InitializeComponent();
        }

        //click event for launching (find item, match to full path, double click to launch)
        private void launchApp_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            int indexSelectedApp = listApplications.SelectedIndex;

            ProcessStartInfo launchInfo = new ProcessStartInfo {
                FileName = hasExeFiles[indexSelectedApp],
                UseShellExecute = true                
            };

            Process.Start(launchInfo);



        }

        //YOU NEED TO ADD EXCEPTION HANDLING HERE. WHAT HAPPENS IF NO DIRECTORY?
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
                        listApplications.Items.Add(results);
                }
                scanExeFiles(folderName);

            }


        }

        //Separate method to stop from scanning too far into subdirectories 
        private void scanExeFiles(string exePath, int depth = 0) {
            int maxDepth = 2;
            if (Directory.Exists(exePath)) { 
                exeFileTypes = Directory.GetFiles(exePath, "*.exe", SearchOption.TopDirectoryOnly);

                foreach(String file in exeFileTypes) {
                    hasExeFiles.Add(file);
                    String results = Path.GetFileNameWithoutExtension(file);
                    listApplications.Items.Add(results);
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
