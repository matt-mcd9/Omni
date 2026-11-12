using System.Collections.Generic;

namespace Omni.Models
{
    public class UserProfile
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Theme { get; set; } = "Dark";
        public string LayoutPreference { get; set; } = "Grid";

        public List<Music> Music { get; set; } = new();
        public List<Video> Videos { get; set; } = new();
        public List<Game> Games { get; set; } = new();
        public List<Software> Software { get; set; } = new();
        public List<Playlist> Playlists { get; set; } = new();
        public List<Favourite> Favourites { get; set; } = new();
        public List<LaunchHistory> LaunchHistory { get; set; } = new();
    }
}
