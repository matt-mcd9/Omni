using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omni.Models
{
    [Table("Video")]
    public class Video
    {
        [Key, Column("Video_id")]
        public int VideoId { get; set; }

        [Column("User_id")]
        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public UserProfile? User { get; set; }
    }
}