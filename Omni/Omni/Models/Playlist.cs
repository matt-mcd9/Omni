using System.Collections.Generic;

namespace Omni.Models
{
    public class Playlist
    {
        public int PlaylistId { get; set; }
        public int UserId { get; set; }
        public string PlaylistName { get; set; } = string.Empty;

        public UserProfile? User { get; set; }
        public List<PlaylistItem> Items { get; set; } = new();
    }
}
