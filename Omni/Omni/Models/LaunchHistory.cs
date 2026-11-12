using System;

namespace Omni.Models
{
    public class LaunchHistory
    {
        public int HistoryId { get; set; }
        public int UserId { get; set; }
        public DateTime LaunchedDate { get; set; }

        public UserProfile? User { get; set; }
        public string MediaType { get; set; } = string.Empty;
        public int ItemId { get; set; }    }
}
