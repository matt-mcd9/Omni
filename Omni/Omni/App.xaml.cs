using System.Windows;
using Microsoft.EntityFrameworkCore;
using Omni.Data;
using Omni.Models;

namespace Omni
{
    public partial class App : Application
    {   // The user that is "logged in" for this session.
        // It's static so any page or window can reach it with App.CurrentUser
        // without passing the user around between pages.
        // "private set" means only App can change it, so a page can't
        // accidentally replace the active user.
        // "= null!" tells the compiler "trust me, this gets set in OnStartup
        // before anything uses it", which silences the nullable warning.
        public static UserProfile CurrentUser { get; private set; } = null!;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeManager.LoadSaved();

            using var db = new OmniDbContext();
            db.Database.Migrate();
            // Make sure a default user exists and remember it for the session.
            // Favourites, launch history and preferences all need a UserId,
            // so the app needs at least one user row before any page loads.
            CurrentUser = new OmniService().GetOrCreateDefaultUser();
        }
    }
}
