using System.Windows;
using Microsoft.EntityFrameworkCore;
using Omni.Data;

namespace Omni
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using var db = new OmniDbContext();
            db.Database.Migrate();
        }
    }
}
