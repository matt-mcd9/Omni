using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omni.Models
{
    [Table("Music")]
    public class Music
    {
        [Key, Column("Music_id")]
        public int MusicId { get; set; }

        [Column("User_id")]
        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public UserProfile? User { get; set; }
    }
}