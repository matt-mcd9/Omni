using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Omni.Data;
using Omni.Models;
using Path = System.IO.Path;

namespace Omni.Pages
{
    public partial class SoftwarePage : UserControl
    {
        public SoftwarePage()
        {
            InitializeComponent();
            Loaded += SoftwarePage_Loaded;
        }

        private async void SoftwarePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadApplicationAsync();
        }

        private async Task LoadApplicationAsync()
        {
            List<MediaItem> applications =
                await DatabaseService.GetLibraryAsync(
                    category: "Software"
                );

            listApplications.ItemsSource = applications;
            listApplications.DisplayMemberPath =
                nameof(MediaItem.Title);
        }

        private async void selectFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new();
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string folderName = dialog.FolderName;

            if (!Directory.Exists(folderName))
            {
                MessageBox.Show("The selected folder does not exist.");
                return;
            }

            string[] applicationFiles = Directory.GetFiles(folderName,
                "*.lnk",
                SearchOption.AllDirectories
            );

            foreach (string filePath in applicationFiles)
            {
                string title =
                    Path.GetFileNameWithoutExtension(filePath);

                await DatabaseService.AddLibraryItemAsync(
                    title,
                    filePath,
                    "Software",
                    "Shortcut"
                );

                await LoadApplicationAsync();

                MessageBox.Show(
                    $"{applicationFiles.Length} applications were found."
                );
            }

        }

        private async void launchApp_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listApplications.SelectedItem is not MediaItem selectedItem)
            {
                return;
            }

            if (!File.Exists(selectedItem.FilePath))
            {
                MessageBox.Show("The selected file does not exist.");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = selectedItem.FilePath,
                UseShellExecute = true
            });

            await DatabaseService.RecordLaunchAsync(
                selectedItem.MediaItemId
            );
        }
    }
}
