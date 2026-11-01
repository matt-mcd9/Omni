namespace Omni.Models
{
    public class Favourite
    {
        public int FavouriteId { get; set; }
        public int UserId { get; set; }
        public int MediaId { get; set; }

        public UserProfile? User { get; set; }
        public MediaItem? MediaItem { get; set; }
    }
}
