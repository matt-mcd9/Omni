using System.Windows;
using Microsoft.EntityFrameworkCore;
using Omni.Data;
using System.Linq;
using Omni.Models;

namespace Omni
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeManager.LoadSaved();

            using var db = new OmniDbContext();
            db.Database.Migrate();

            if (!db.UserProfiles.Any())                                          
            {                                                                    
                db.UserProfiles.AddRange(                                        
                    new UserProfile { UserId = 1, Username = "User 1" },         
                    new UserProfile { UserId = 2, Username = "User 2" });        
                db.SaveChanges();                                                
            }                                                                    

            ThemeManager.LoadFromDatabase(OmniService.CurrentUserId);
            
            
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // A colour change may still be waiting on the Settings page's save timer
            ThemeManager.SavePending();

            base.OnExit(e);
        }
    }
}
