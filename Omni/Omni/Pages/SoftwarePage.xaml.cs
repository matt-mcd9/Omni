using Microsoft.Win32;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Omni.Pages
{
    public partial class SoftwarePage : UserControl
    {
        String[] hasExeFiles;
        public SoftwarePage()
        {
            InitializeComponent();
        }

        //click event for launching (find item, match to full path, launch)
        private void launchApp_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            int indexSelectedApp = listApplications.SelectedIndex;

            ProcessStartInfo launchInfo = new ProcessStartInfo {
                FileName = hasExeFiles[indexSelectedApp],
                UseShellExecute = true                
            };

            Process.Start(launchInfo);
            
                
        }

        private void selectFolder_Click(object sender, EventArgs e) {
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.ShowDialog();
            String folderName = dialog.FolderName;

            if (Directory.Exists(folderName)) {
                hasExeFiles = Directory.GetFiles(folderName, "*.lnk");
                foreach (String file in hasExeFiles) {
                    String results = System.IO.Path.GetFileNameWithoutExtension(file);
                    listApplications.Items.Add(results);

                }
            }
        }

    }
}
