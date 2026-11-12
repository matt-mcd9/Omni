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
    }
}
