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

        public bool IsFavourite(int userId, string mediaType, int itemId)
        {
            using var db = new OmniDbContext();
            return db.Favourites.Any(f => f.UserId == userId && f.MediaType == mediaType && f.ItemId == itemId);
        }

        public List<LibraryItem> GetFavourites(int userId)
        {
            using var db = new OmniDbContext();
            var favourites = db.Favourites.Where(f => f.UserId == userId).ToList();

            return favourites
                .Select(f => FindItem(db, f.MediaType, f.ItemId))
                .OfType<LibraryItem>() // skips items that were deleted
                .ToList();
        }

// Looks the item up in the table MediaType points to
        private static LibraryItem? FindItem(OmniDbContext db, string type, int id) => type switch
        {
            "Music"    => db.Music.Find(id)    is { } m ? new LibraryItem(type, m.MusicId, m.Title, m.FilePath) : null,
            "Video"    => db.Videos.Find(id)   is { } v ? new LibraryItem(type, v.VideoId, v.Title, v.FilePath) : null,
            "Game"     => db.Games.Find(id)    is { } g ? new LibraryItem(type, g.GameId, g.Title, g.FilePath) : null,
            "Software" => db.Software.Find(id) is { } s ? new LibraryItem(type, s.SoftwareId, s.Title, s.FilePath) : null,
            _ => null
        };

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

        public Dictionary<string, int> GetLibraryCounts()
        {
            using var db = new OmniDbContext();
            return new Dictionary<string, int>
            {
                ["Music"] = db.Music.Count(),
                ["Video"] = db.Videos.Count(),
                ["Game"] = db.Games.Count(),
                ["Software"] = db.Software.Count()
            };
        }

        public List<LaunchHistory> GetRecentHistory(int userId, int count = 20)
        {
            using var db = new OmniDbContext();
            return db.LaunchHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.LaunchedDate)
                .Take(count)
                .ToList();
        }
        
        public record LibraryItem(string Type, int Id, string Title, string FilePath);
        
        // scans are saved to user 1. Set to 2 to save to user 2.
        public static int CurrentUserId { get; set; } = 1;

        public int SaveScannedMusic(int userId, IEnumerable<string> paths)
        {
            using var db = new OmniDbContext();
            var known = KnownPaths(db.Music.Where(m => m.UserId == userId).Select(m => m.FilePath));
            var added = NewPaths(paths, known).Select(p => new Music { UserId = userId, Title = TitleOf(p), FilePath = p }).ToList();
            db.Music.AddRange(added);
            db.SaveChanges();
            return added.Count;
        }

        public int SaveScannedVideos(int userId, IEnumerable<string> paths)
        {
            using var db = new OmniDbContext();
            var known = KnownPaths(db.Videos.Where(v => v.UserId == userId).Select(v => v.FilePath));
            var added = NewPaths(paths, known).Select(p => new Video { UserId = userId, Title = TitleOf(p), FilePath = p }).ToList();
            db.Videos.AddRange(added);
            db.SaveChanges();
            return added.Count;
        }

        public int SaveScannedGames(int userId, IEnumerable<string> paths)
        {
            using var db = new OmniDbContext();
            var known = KnownPaths(db.Games.Where(g => g.UserId == userId).Select(g => g.FilePath));
            var added = NewPaths(paths, known).Select(p => new Game { UserId = userId, Title = TitleOf(p), FilePath = p }).ToList();
            db.Games.AddRange(added);
            db.SaveChanges();
            return added.Count;
        }

        public int SaveScannedSoftware(int userId, IEnumerable<string> paths)
        {
            using var db = new OmniDbContext();
            var known = KnownPaths(db.Software.Where(s => s.UserId == userId).Select(s => s.FilePath));
            var added = NewPaths(paths, known).Select(p => new Software { UserId = userId, Title = TitleOf(p), FilePath = p }).ToList();
            db.Software.AddRange(added);
            db.SaveChanges();
            return added.Count;
        }
        
        public void SaveTheme(int userId, string theme)
        {
            using var db = new OmniDbContext();
            var user = db.UserProfiles.Find(userId);
            if (user is null) return;

            user.Theme = theme;
            db.SaveChanges();
        }
        
        public string? GetTheme(int userId)
        {
            using var db = new OmniDbContext();
            return db.UserProfiles
                .Where(u => u.UserId == userId)
                .Select(u => u.Theme)
                .FirstOrDefault();
        }
        
        private static HashSet<string> KnownPaths(IQueryable<string> paths) =>
            new(paths.ToList(), StringComparer.OrdinalIgnoreCase);

        private static IEnumerable<string> NewPaths(IEnumerable<string> paths, HashSet<string> known) =>
            paths.Where(p => known.Add(p));

        private static string TitleOf(string path) => System.IO.Path.GetFileNameWithoutExtension(path);
        
        public List<string> GetMusicPaths(int userId)
        {
            using var db = new OmniDbContext();
            return db.Music.Where(m => m.UserId == userId).OrderBy(m => m.Title).Select(m => m.FilePath).ToList();
        }

        public List<string> GetVideoPaths(int userId)
        {
            using var db = new OmniDbContext();
            return db.Videos.Where(v => v.UserId == userId).OrderBy(v => v.Title).Select(v => v.FilePath).ToList();
        }

        public List<string> GetGamePaths(int userId)
        {
            using var db = new OmniDbContext();
            return db.Games.Where(g => g.UserId == userId).OrderBy(g => g.Title).Select(g => g.FilePath).ToList();
        }

        public List<string> GetSoftwarePaths(int userId)
        {
            using var db = new OmniDbContext();
            return db.Software.Where(s => s.UserId == userId).OrderBy(s => s.Title).Select(s => s.FilePath).ToList();
        }

        public void RemoveMusic(int userId, string path)
        {
            using var db = new OmniDbContext();
            db.Music.Where(m => m.UserId == userId && m.FilePath == path).ExecuteDelete();
        }
    }
}
