using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Omni.Models;

namespace Omni.Data
{
    public class OmniService
    {
        public bool AddFavourite(int userId, int mediaId)
        {
            using var db = new OmniDbContext();

            bool alreadyExists = db.Favourites
                .Any(f => f.UserId == userId && f.MediaId == mediaId);
            if (alreadyExists) return false;

            db.Favourites.Add(new Favourite { UserId = userId, MediaId = mediaId });
            db.SaveChanges();
            return true;
        }

        public bool RemoveFavourite(int userId, int mediaId)
        {
            using var db = new OmniDbContext();

            var fav = db.Favourites
                .FirstOrDefault(f => f.UserId == userId && f.MediaId == mediaId);
            if (fav is null) return false;

            db.Favourites.Remove(fav);
            db.SaveChanges();
            return true;
        }

        public bool IsFavourite(int userId, int mediaId)
        {
            using var db = new OmniDbContext();
            return db.Favourites.Any(f => f.UserId == userId && f.MediaId == mediaId);
        }

        public List<MediaItem> GetFavourites(int userId)
        {
            using var db = new OmniDbContext();
            return db.Favourites
                .Where(f => f.UserId == userId)
                .Include(f => f.MediaItem)
                .Select(f => f.MediaItem!)
                .ToList();
        }

        public void RecordLaunch(int userId, int mediaId)
        {
            using var db = new OmniDbContext();
            db.LaunchHistory.Add(new LaunchHistory
            {
                UserId = userId,
                MediaId = mediaId,
                LaunchedDate = DateTime.Now
            });
            db.SaveChanges();
        }

        // Number of media items per CategoryId
        public Dictionary<int, int> GetCategoryCounts()
        {
            using var db = new OmniDbContext();
            return db.MediaItems
                .GroupBy(m => m.CategoryId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionary(x => x.Key, x => x.Count);
        }

        public List<LaunchHistory> GetRecentHistory(int userId, int count = 20)
        {
            using var db = new OmniDbContext();
            return db.LaunchHistory
                .Where(h => h.UserId == userId)
                .Include(h => h.MediaItem)
                .OrderByDescending(h => h.LaunchedDate)
                .Take(count)
                .ToList();
        }
               
        // The username for the profile that is created automatically.
        // It's a constant so the name is defined in one place, and other code
        // can check against OmniService.DefaultUsername instead of a typed string.
        public const string DefaultUsername = "Default";

        // Returns the default user, creating it the first time the app runs.
        // Called once on startup from App.OnStartup.
        public UserProfile GetOrCreateDefaultUser()
        {
            // A new short-lived DbContext per method, same pattern as the other
            // methods in this class. "using" disposes it (closing the DB
            // connection) when the method ends.
            using var db = new OmniDbContext();

            // Look for an existing default user first.
            // Username has a UNIQUE index (see OmniDbContext.OnModelCreating), so
            // inserting a second "Default" user on every launch would throw an
            // error. Checking first makes this safe to call every startup.
            var user = db.UserProfiles.FirstOrDefault(u => u.Username == DefaultUsername);
            if (user is not null) return user;

            // First launch: no user yet, so create one.
            // Theme ("Dark") and LayoutPreference ("Grid") don't need to be set
            // here because UserProfile already gives them default values.
            user = new UserProfile { Username = DefaultUsername };
            db.UserProfiles.Add(user);

            // Writes the new row to the database. SQLite generates the UserId,
            // and EF copies it back into user.UserId automatically.
            db.SaveChanges();
            return user;
        }

        // Loads a user's current data (including Theme and LayoutPreference)
        // fresh from the database. Use this when a page needs to read prefs.
        // Returns null if no user has that ID, hence the "?" on the return type.
        public UserProfile? GetUser(int userId)
        {
            using var db = new OmniDbContext();

            // Find() looks up a row by its primary key (UserId). It's the
            // simplest and fastest way to fetch a single row by ID.
            return db.UserProfiles.Find(userId);
        }

        // Saves new preference values for a user, e.g. when they change the
        // theme or layout on the Settings page.
        public void UpdatePreferences(int userId, string theme, string layout)
        {
            using var db = new OmniDbContext();

            // Load the user so EF starts tracking it. EF only saves changes
            // to entities it is tracking.
            var user = db.UserProfiles.Find(userId);

            // If the user doesn't exist, do nothing instead of crashing.
            if (user is null) return;

            // Change the values on the tracked object...
            user.Theme = theme;
            user.LayoutPreference = layout;

            // ...and EF notices what changed and runs an UPDATE for only those columns.
            db.SaveChanges();
        }
        
        // Saves just the theme for a user. Settings only changes the theme, so
        // this avoids having to pass the layout in too (as UpdatePreferences needs).
        public void UpdateTheme(int userId, string theme)
        {
            using var db = new OmniDbContext();
            
            var user = db.UserProfiles.Find(userId);
            
            if (user is null) return;
            
            user.Theme = theme;
            db.SaveChanges();
        }








    
    }
}
