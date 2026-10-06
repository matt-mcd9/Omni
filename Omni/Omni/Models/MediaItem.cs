using System.Collections.Generic;

namespace Omni.Models
{
    public class MediaItem
    {
        public int MediaId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public int CategoryId { get; set; }

        public UserProfile? User { get; set; }
        public Category? Category { get; set; }
        public List<Favourite> Favourites { get; set; } = new();
        public List<LaunchHistory> LaunchHistory { get; set; } = new();
        public List<PlaylistItem> PlaylistItems { get; set; } = new();
    }
}
