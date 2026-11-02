using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Omni.Models;
using System.Collections;
using Omni.Pages;
namespace Omni.Data
{
    public class OmniService
    {
        public bool AddFavourite(int userId, int playlistItemId)
        {
            using var db = new OmniDbContext();

            bool alreadyExists = db.Favourites
                .Any(f => f.UserId == userId && f.PlaylistItemId == playlistItemId);
            if (alreadyExists) return false;

            db.Favourites.Add(new Favourite { UserId = userId, PlaylistItemId = playlistItemId });
            db.SaveChanges();
            return true;
        }

        public bool RemoveFavourite(int userId, int playlistItemId)
        {
            using var db = new OmniDbContext();

            var fav = db.Favourites
                .FirstOrDefault(f => f.UserId == userId && f.PlaylistItemId == playlistItemId);
            if (fav is null) return false;

            db.Favourites.Remove(fav);
            db.SaveChanges();
            return true;
        }

        public bool IsFavourite(int userId, int playlistItemId)
        {
            using var db = new OmniDbContext();
            return db.Favourites.Any(f => f.UserId == userId && f.PlaylistItemId == playlistItemId);
        }

        public List<PlaylistItem> GetFavourites(int userId)
        {
            using var db = new OmniDbContext();
            return db.Favourites
                .Where(f => f.UserId == userId)
                .Include(f => f.PlaylistItem)
                .Select(f => f.PlaylistItem!)
                .ToList();
        }

        public void RecordLaunch(int userId, int playlistItemId)
        {
            using var db = new OmniDbContext();
            db.LaunchHistory.Add(new LaunchHistory
            {
                UserId = userId,
                PlaylistItemId = playlistItemId,
                LaunchedDate = DateTime.Now
            });
            db.SaveChanges();
        }
        // Records a launch for pages that only know the file path.
        // Does nothing if the file was never saved to PlaylistItems by a folder scan.
        public void RecordLaunchByPath(int userId, string filePath)
        {
            using var db = new OmniDbContext();

            // Find the saved PlaylistItem for this file, since LaunchHistory needs its PlaylistItemId
            var item = db.PlaylistItems.FirstOrDefault(i => i.Playlist!.UserId == userId && i.FilePath == filePath);
            if (item is null) return;

            // Same as RecordLaunch: one history row per launch, stamped with the current time
            db.LaunchHistory.Add(new LaunchHistory
            {
                UserId = userId,
                PlaylistItemId = item.PlaylistItemId,
                LaunchedDate = DateTime.Now
            });
            db.SaveChanges();
        }

        // Each user has one playlist, created the first time it's needed
        private static Playlist GetOrCreateLibrary(OmniDbContext db, int userId)
        {
            var playlist = db.Playlists.FirstOrDefault(p => p.UserId == userId);
            if (playlist is not null) return playlist;

            playlist = new Playlist { UserId = userId, PlaylistName = "Library" };
            db.Playlists.Add(playlist);
            db.SaveChanges();
            return playlist;
        }

        // Saves the items a page found in a folder scan as PlaylistItems.
        // Files already in the database (same path) are skipped, so scanning
        // the same folder twice doesn't add duplicates.
        public int SaveLibraryItems(int userId, int categoryId, string mediaType, ArrayList found)
        {
            // A new short-lived DbContext, same pattern as the other methods
            using var db = new OmniDbContext();
            var playlist = GetOrCreateLibrary(db, userId);

            // Paths this user already has saved in this category.
            // A HashSet makes "is this path already saved?" a fast check.
            var existingPaths = db.PlaylistItems
                .Where(i => i.PlaylistId == playlist.PlaylistId && i.CategoryId == categoryId)
                .Select(i => i.FilePath)
                .ToHashSet();

            // New items go after whatever is already in the playlist
            int position = db.PlaylistItems.Count(i => i.PlaylistId == playlist.PlaylistId);
            int added = 0;
            foreach (SoftwarePage.AppEntry entry in found)
            {
                // HashSet.Add returns false if the path is already there,
                // which also skips a file that appears twice in the same scan
                if (!existingPaths.Add(entry.FullPath)) continue;

                // Queue a new row; nothing is written until SaveChanges below
                db.PlaylistItems.Add(new PlaylistItem
                {
                    PlaylistId = playlist.PlaylistId,
                    Title = entry.Name,
                    FilePath = entry.FullPath,
                    MediaType = mediaType,
                    CategoryId = categoryId,
                    Position = position++
                });
                added++;
            }

            // One SaveChanges for the whole scan instead of one per file
            db.SaveChanges();
            return added;
        }

        // Number of playlist items per CategoryId
        public Dictionary<int, int> GetCategoryCounts()
        {
            using var db = new OmniDbContext();
            return db.PlaylistItems
                .GroupBy(i => i.CategoryId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionary(x => x.Key, x => x.Count);
        }

        public List<LaunchHistory> GetRecentHistory(int userId, int count = 20)
        {
            using var db = new OmniDbContext();
            return db.LaunchHistory
                .Where(h => h.UserId == userId)
                .Include(h => h.PlaylistItem)
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
