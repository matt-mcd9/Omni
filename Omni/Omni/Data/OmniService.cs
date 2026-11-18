using System;
using System.Collections.Generic;
using System.Linq;
using LibVLCSharp.Shared;
using Microsoft.EntityFrameworkCore;
using Omni.Models;

namespace Omni.Data
{
    public class OmniService
    {
        //-------------------------------------------------------------------------------------------------------------//
        // Shared model and user state
        public record LibraryItem(string Type, int Id, string Title, string FilePath);

        // Scans are saved to the current user.
        public static int CurrentUserId { get; set; } = 1;

        //-------------------------------------------------------------------------------------------------------------//
        // Favourite management
        // Adds a media item to the user's favourites if it is not already there.
        public bool AddFavourite(int userId, string mediaType, int itemId)
        {
            using var db = new OmniDbContext();

            bool alreadyExists = db.Favourites
                .Any(f => f.UserId == userId && f.MediaType == mediaType && f.ItemId == itemId);
            if (alreadyExists) return false;

            db.Favourites.Add(new Favourite { UserId = userId, MediaType = mediaType, ItemId = itemId });
            db.SaveChanges();
            return true;
        }

        // Removes a media item from the user's favourites.
        public bool RemoveFavourite(int userId, string mediaType, int itemId)
        {
            using var db = new OmniDbContext();

            var fav = db.Favourites
                .FirstOrDefault(f => f.UserId == userId && f.MediaType == mediaType && f.ItemId == itemId);
            if (fav is null) return false;

            db.Favourites.Remove(fav);
            db.SaveChanges();
            return true;
        }

        // Checks whether a media item is in the user's favourites.
        public bool IsFavourite(int userId, string mediaType, int itemId)
        {
            using var db = new OmniDbContext();
            return db.Favourites.Any(f => f.UserId == userId && f.MediaType == mediaType && f.ItemId == itemId);
        }

        // Finds the library ID of a file the user has saved, or null if it isn't in their library.
        // Paths are matched ignoring case, as when saving scans.
        public int? GetItemId(int userId, string mediaType, string path)
        {
            using var db = new OmniDbContext();
            string lower = path.ToLower();
            return mediaType switch
            {
                "Music" => db.Music.Where(m => m.UserId == userId && m.FilePath.ToLower() == lower).Select(m => (int?)m.MusicId).FirstOrDefault(),
                "Video" => db.Videos.Where(v => v.UserId == userId && v.FilePath.ToLower() == lower).Select(v => (int?)v.VideoId).FirstOrDefault(),
                "Game" => db.Games.Where(g => g.UserId == userId && g.FilePath.ToLower() == lower).Select(g => (int?)g.GameId).FirstOrDefault(),
                "Software" => db.Software.Where(s => s.UserId == userId && s.FilePath.ToLower() == lower).Select(s => (int?)s.SoftwareId).FirstOrDefault(),
                _ => null
            };
        }

        // Gets the user's favourites and resolves them to their library items.
        public List<LibraryItem> GetFavourites(int userId)
        {
            using var db = new OmniDbContext();
            var favourites = db.Favourites.Where(f => f.UserId == userId).ToList();

            return favourites
                .Select(f => FindItem(db, f.MediaType, f.ItemId))
                .OfType<LibraryItem>()
                .ToList();
        }

        //-------------------------------------------------------------------------------------------------------------//
        // Library overview
        // Gets the number of items stored in each of the user's libraries.
        public Dictionary<string, int> GetLibraryCounts(int userId)
        {
            using var db = new OmniDbContext();
            return new Dictionary<string, int>
            {
                ["Music"] = db.Music.Count(m => m.UserId == userId),
                ["Video"] = db.Videos.Count(v => v.UserId == userId),
                ["Game"] = db.Games.Count(g => g.UserId == userId),
                ["Software"] = db.Software.Count(s => s.UserId == userId)
            };
        }

        //-------------------------------------------------------------------------------------------------------------//
        // Music management
        // Gets the user's music file paths ordered by title.
        public List<string> GetMusicPaths(int userId)
        {
            using var db = new OmniDbContext();
            return db.Music
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.Title)
                .Select(m => m.FilePath)
                .ToList();
        }

        // Adds a music file to the user's library.
        public void AddMusic(int userId, string path)
        {
            using var db = new OmniDbContext();
            db.Music.Add(new Music { UserId = userId, Title = TitleOf(path), FilePath = path });
            db.SaveChanges();
        }

        // Removes a music file from the user's library.
        public void RemoveMusic(int userId, string path)
        {
            using var db = new OmniDbContext();
            db.Music.Where(m => m.UserId == userId && m.FilePath == path).ExecuteDelete();
            db.SaveChanges();
        }

        // Removes all music files from the user's library.
        public void RemoveAllMusic(int userId)
        {
            using var db = new OmniDbContext();
            db.Music.Where(m => m.UserId == userId).ExecuteDelete();
        }

        // Adds new scanned music files that are not already in the user's library.
        public int SaveScannedMusic(int userId, IEnumerable<string> paths)
        {
            using var db = new OmniDbContext();
            var known = KnownPaths(db.Music.Where(m => m.UserId == userId).Select(m => m.FilePath));
            var added = NewPaths(paths, known)
                .Select(p => new Music { UserId = userId, Title = TitleOf(p), FilePath = p })
                .ToList();
            db.Music.AddRange(added);
            db.SaveChanges();
            return added.Count;
        }

        //-------------------------------------------------------------------------------------------------------------//
        // Video management
        // Gets the user's video file paths ordered by title.
        public List<string> GetVideoPaths(int userId)
        {
            using var db = new OmniDbContext();
            return db.Videos
                .Where(v => v.UserId == userId)
                .OrderBy(v => v.Title)
                .Select(v => v.FilePath)
                .ToList();
        }

        // Adds a video file to the user's library.
        public void AddVideo(int userId, string path)
        {
            using var db = new OmniDbContext();
            db.Videos.Add(new Video { UserId = userId, Title = TitleOf(path), FilePath = path });
            db.SaveChanges();
        }

        // Removes a video file from the user's library.
        public void RemoveVideo(int userId, string path)
        {
            using var db = new OmniDbContext();
            db.Videos.Where(v => v.UserId == userId && v.FilePath == path).ExecuteDelete();
            db.SaveChanges();
        }

        // Removes all video files from the user's library.
        public void RemoveAllVideos(int userId)
        {
            using var db = new OmniDbContext();
            db.Videos.Where(v => v.UserId == userId).ExecuteDelete();
        }

        // Adds new scanned video files that are not already in the user's library.
        public int SaveScannedVideos(int userId, IEnumerable<string> paths)
        {
            using var db = new OmniDbContext();
            var known = KnownPaths(db.Videos.Where(v => v.UserId == userId).Select(v => v.FilePath));
            var added = NewPaths(paths, known)
                .Select(p => new Video { UserId = userId, Title = TitleOf(p), FilePath = p })
                .ToList();
            db.Videos.AddRange(added);
            db.SaveChanges();
            return added.Count;
        }

        //-------------------------------------------------------------------------------------------------------------//
        // Game management
        // Gets the user's game file paths ordered by title.
        public List<string> GetGamePaths(int userId)
        {
            using var db = new OmniDbContext();
            return db.Games
                .Where(g => g.UserId == userId)
                .OrderBy(g => g.Title)
                .Select(g => g.FilePath)
                .ToList();
        }

        // Adds a game file to the user's library.
        public void AddGame(int userId, string path)
        {
            using var db = new OmniDbContext();
            db.Games.Add(new Game { UserId = userId, Title = TitleOf(path), FilePath = path });
            db.SaveChanges();
        }

        // Removes a game file from the user's library.
        public void RemoveGame(int userId, string path)
        {
            using var db = new OmniDbContext();
            db.Games.Where(g => g.UserId == userId && g.FilePath == path).ExecuteDelete();
            db.SaveChanges();
        }

        // Adds new scanned game files that are not already in the user's library.
        public int SaveScannedGames(int userId, IEnumerable<string> paths)
        {
            using var db = new OmniDbContext();
            var known = KnownPaths(db.Games.Where(g => g.UserId == userId).Select(g => g.FilePath));
            var added = NewPaths(paths, known)
                .Select(p => new Game { UserId = userId, Title = TitleOf(p), FilePath = p })
                .ToList();
            db.Games.AddRange(added);
            db.SaveChanges();
            return added.Count;
        }

        //-------------------------------------------------------------------------------------------------------------//
        // Software management
        // Gets the user's software file paths ordered by title.
        public List<string> GetSoftwarePaths(int userId)
        {
            using var db = new OmniDbContext();
            return db.Software
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.Title)
                .Select(s => s.FilePath)
                .ToList();
        }

        // Adds a software file to the user's library.
        public void AddSoftware(int userId, string path)
        {
            using var db = new OmniDbContext();
            db.Software.Add(new Software { UserId = userId, Title = TitleOf(path), FilePath = path });
            db.SaveChanges();
        }

        // Removes a software file from the user's library.
        public void RemoveSoftware(int userId, string path)
        {
            using var db = new OmniDbContext();
            db.Software.Where(s => s.UserId == userId && s.FilePath == path).ExecuteDelete();
            db.SaveChanges();
        }

        // Adds new scanned software files that are not already in the user's library.
        public int SaveScannedSoftware(int userId, IEnumerable<string> paths)
        {
            using var db = new OmniDbContext();
            var known = KnownPaths(db.Software.Where(s => s.UserId == userId).Select(s => s.FilePath));
            var added = NewPaths(paths, known)
                .Select(p => new Software { UserId = userId, Title = TitleOf(p), FilePath = p })
                .ToList();
            db.Software.AddRange(added);
            db.SaveChanges();
            return added.Count;
        }

        //-------------------------------------------------------------------------------------------------------------//
        // Launch history
        // Records a media launch for the user.
        public void RecordLaunch(int userId, string mediaType, int itemId)
        {
            using var db = new OmniDbContext();
            db.LaunchHistory.Add(new LaunchHistory
            {
                UserId = userId,
                MediaType = mediaType,
                ItemId = itemId,
                LaunchedDate = DateTime.Now
            });
            db.SaveChanges();
        }

        // Records opening a file from the user's library. Pages only know the path, so this finds
        // the item first; files that aren't in the library aren't recorded.
        public void RecordLaunchByPath(int userId, string mediaType, string path)
        {
            int? itemId = GetItemId(userId, mediaType, path);
            if (itemId is null) return;

            RecordLaunch(userId, mediaType, itemId.Value);

            // Only the most recent launches are ever shown, so older ones are dropped
            using var db = new OmniDbContext();
            var old = db.LaunchHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.LaunchedDate)
                .Skip(MaxHistoryPerUser)
                .Select(h => h.HistoryId)
                .ToList();
            if (old.Count > 0)
                db.LaunchHistory.Where(h => old.Contains(h.HistoryId)).ExecuteDelete();
        }

        private const int MaxHistoryPerUser = 100;

        // Gets what the user opened most recently, newest first, each item once.
        // Items removed from the library since are skipped.
        public List<LibraryItem> GetRecentItems(int userId, int count)
        {
            using var db = new OmniDbContext();
            var history = db.LaunchHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.LaunchedDate)
                .ToList();

            return history
                .DistinctBy(h => (h.MediaType, h.ItemId))
                .Select(h => FindItem(db, h.MediaType, h.ItemId))
                .OfType<LibraryItem>()
                .Take(count)
                .ToList();
        }

        // Gets the user's most recent launch records.
        public List<LaunchHistory> GetRecentHistory(int userId, int count = 20)
        {
            using var db = new OmniDbContext();
            return db.LaunchHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.LaunchedDate)
                .Take(count)
                .ToList();
        }

        //-------------------------------------------------------------------------------------------------------------//
        // Theme management
        // Saves the user's layout preference.
        public void SaveLayoutPreference(int userId, string layoutPreference)
        {
            using var db = new OmniDbContext();
            var user = db.UserProfiles.Find(userId);
            if (user is null) return;

            user.LayoutPreference = layoutPreference;
            db.SaveChanges();
        }

        // Saves the user's selected theme.
        public void SaveTheme(int userId, string theme)
        {
            using var db = new OmniDbContext();
            var user = db.UserProfiles.Find(userId);
            if (user is null) return;

            user.Theme = theme;
            db.SaveChanges();
        }

        // Gets the theme saved for the user.
        public string? GetTheme(int userId)
        {
            using var db = new OmniDbContext();
            return db.UserProfiles
                .Where(u => u.UserId == userId)
                .Select(u => u.Theme)
                .FirstOrDefault();
        }

        //-------------------------------------------------------------------------------------------------------------//
        // User management
        // Gets all user profiles.
        public List<UserProfile> GetAllUsers()
        {
            using var db = new OmniDbContext();
            return db.UserProfiles.ToList();
        }

        // Gets one user profile, or null if there is no user with this ID.
        public UserProfile? GetUser(int userId)
        {
            using var db = new OmniDbContext();
            return db.UserProfiles.Find(userId);
        }

        // Creates a user with this name and returns it (the database picks the ID),
        // or returns null if the name is already taken, ignoring case.
        public UserProfile? CreateUser(string username)
        {
            using var db = new OmniDbContext();
            string name = username.Trim();
            if (db.UserProfiles.Any(u => u.Username.ToLower() == name.ToLower())) return null;

            var user = new UserProfile { Username = name };
            db.UserProfiles.Add(user);
            db.SaveChanges();
            return user;
        }

        // Adds a user profile with the specified ID.
        public void AddUser(int userId)
        {
            using var db = new OmniDbContext();
            db.UserProfiles.Add(new UserProfile { UserId = userId });
            db.SaveChanges();
        }

        // Removes a user and all of the user's library records.
        public void RemoveUser(int userId)
        {
            using var db = new OmniDbContext();
            db.UserProfiles.Where(u => u.UserId == userId).ExecuteDelete();
            db.Music.Where(m => m.UserId == userId).ExecuteDelete();
            db.Videos.Where(v => v.UserId == userId).ExecuteDelete();
            db.Games.Where(g => g.UserId == userId).ExecuteDelete();
            db.Software.Where(s => s.UserId == userId).ExecuteDelete();
            db.SaveChanges();
        }

        //-------------------------------------------------------------------------------------------------------------//
        // Private helper methods for scanning and saving new media items
        // Resolves a favourite's media type and ID to the corresponding library item.
        private static LibraryItem? FindItem(OmniDbContext db, string type, int id) => type switch
        {
            "Music" => db.Music.Find(id) is { } m ? new LibraryItem(type, m.MusicId, m.Title, m.FilePath) : null,
            "Video" => db.Videos.Find(id) is { } v ? new LibraryItem(type, v.VideoId, v.Title, v.FilePath) : null,
            "Game" => db.Games.Find(id) is { } g ? new LibraryItem(type, g.GameId, g.Title, g.FilePath) : null,
            "Software" => db.Software.Find(id) is { } s ? new LibraryItem(type, s.SoftwareId, s.Title, s.FilePath) : null,
            _ => null
        };

        // Creates a case-insensitive set of paths already stored in the database.
        private static HashSet<string> KnownPaths(IQueryable<string> paths) =>
            new(paths.ToList(), StringComparer.OrdinalIgnoreCase);

        // Returns only paths that are not already in the known path set.
        private static IEnumerable<string> NewPaths(IEnumerable<string> paths, HashSet<string> known) =>
            paths.Where(p => known.Add(p));

        // Gets a display title from a file path.
        private static string TitleOf(string path) => System.IO.Path.GetFileNameWithoutExtension(path);
    }
}
