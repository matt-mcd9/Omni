using System.Collections.Generic;

namespace Omni.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public List<PlaylistItem> Items { get; set; } = new();
    }
}
