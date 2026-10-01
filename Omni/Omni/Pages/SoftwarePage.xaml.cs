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

        private void selectFolder_Click(object sender, EventArgs e) {
            hasExeFiles.Clear();
            listApplications.Items.Clear();

            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.ShowDialog();
            String folderName = dialog.FolderName;

            if (Directory.Exists(folderName)) {
                //hasExeFiles = Directory.GetFiles(folderName, "*.lnk", SearchOption.AllDirectories);
                //hasExeFiles = Directory.GetFiles(folderName, "*.exe", SearchOption.AllDirectories);
                fileTypes = Directory.GetFiles(folderName, "*", SearchOption.AllDirectories);

                foreach (String file in fileTypes) {

                    String fileType = Path.GetExtension(file);
                    if (fileType == ".lnk" || fileType == ".exe") {
                        hasExeFiles.Add(file);
                        String results = System.IO.Path.GetFileNameWithoutExtension(file);
                        listApplications.Items.Add(results);

                    }


                }

            }


        }

    }
}
