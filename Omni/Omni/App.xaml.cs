using Omni.Data;
using System.Windows;

namespace Omni
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Initialize the database

            try
            {
                await DatabaseService.InitializeAsync();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    $"The Omni database could not be initialized.\n\n{exception.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown();
            }
        }
    }
}
