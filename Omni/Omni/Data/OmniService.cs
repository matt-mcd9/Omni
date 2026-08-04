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
    }
}
