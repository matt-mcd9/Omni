namespace Omni.Models
{
    public class Favourite
    {
        public int FavouriteId { get; set; }
        public int UserId { get; set; }
        public int PlaylistItemId { get; set; }

        public UserProfile? User { get; set; }
        public PlaylistItem? PlaylistItem { get; set; }
    }
}
