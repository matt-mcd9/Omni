using System.Windows;
using Microsoft.EntityFrameworkCore;
using Omni.Data;
using Omni.Pages;

namespace Omni
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using (var db = new OmniDbContext())
                db.Database.Migrate();

            // Pick or create a user before the main window opens
            ShowUserSelect();
        }

        // Shows the start-up page; picking or creating a user opens the main window as them.
        // Closing it without choosing closes the app, as it is then the only window.
        private void ShowUserSelect()
        {
            var userSelect = new UserSelectWindow();
            userSelect.UserChosen += user =>
            {
                StartAs(user.UserId);
                userSelect.Close();
            };
            userSelect.Show();
        }

        private void StartAs(int userId)
        {
            OmniService.CurrentUserId = userId;

            // The theme is stored per user, so it can only be loaded once we know who is using the app
            ThemeManager.ImportOldSettingsFile(userId);
            ThemeManager.Load(userId);

            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        // Settings > Switch user: closes the main window and goes back to the start-up page
        public void SwitchUser()
        {
            // Music keeps playing after leaving its page, but shouldn't carry on for the next user
            MusicPage.StopAll();
            ThemeManager.Reset();

            // Opened before the main window closes, so the app always has a window and doesn't shut down
            var main = MainWindow;
            ShowUserSelect();
            main?.Close();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // A colour change may still be waiting on the Settings page's save timer
            ThemeManager.SavePending();

            base.OnExit(e);
        }
    }
}
