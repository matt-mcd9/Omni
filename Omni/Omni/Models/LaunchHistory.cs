using System;

namespace Omni.Models
{
    public class LaunchHistory
    {
        public int HistoryId { get; set; }
        public int UserId { get; set; }
        public int MediaId { get; set; }
        public DateTime LaunchedDate { get; set; }

        public UserProfile? User { get; set; }
        public MediaItem? MediaItem { get; set; }
    }
}
