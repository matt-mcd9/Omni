namespace Omni.Models
{
    public class PlaylistItem
    {
        public int PlaylistItemId { get; set; }
        public int PlaylistId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public int Position { get; set; }

        public Playlist? Playlist { get; set; }
        public Category? Category { get; set; }
    }
}
