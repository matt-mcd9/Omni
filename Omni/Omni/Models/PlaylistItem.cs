namespace Omni.Models
{
    // Join table between Playlist and MediaItem — not shown in the original
    // sample schema, but required for a playlist to actually reference media.
    public class PlaylistItem
    {
        public int PlaylistItemId { get; set; }
        public int PlaylistId { get; set; }
        public int MediaId { get; set; }
        public int Position { get; set; }

        public Playlist? Playlist { get; set; }
        public MediaItem? MediaItem { get; set; }
    }
}
