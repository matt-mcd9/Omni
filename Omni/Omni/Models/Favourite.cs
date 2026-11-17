namespace Omni.Models
{
    public class Favourite
    {
        public int FavouriteId { get; set; }
        public int UserId { get; set; }
        
        public string MediaType { get; set; } = string.Empty; // "Music", "Video", "Game" or "Software"
        
        public int ItemId { get; set; }
        public UserProfile? User { get; set; }
    }
}
