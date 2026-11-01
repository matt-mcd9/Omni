using System.Collections.Generic;

namespace Omni.Models
{
    public class UserProfile
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Theme { get; set; } = "Dark";
        public string LayoutPreference { get; set; } = "Grid";

        public List<MediaItem> MediaItems { get; set; } = new();
        public List<Playlist> Playlists { get; set; } = new();
        public List<Favourite> Favourites { get; set; } = new();
        public List<LaunchHistory> LaunchHistory { get; set; } = new();
    }
}
