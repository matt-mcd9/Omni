using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omni.Models
{
    [Table("Software")]
    public class Software
    {
        [Key, Column("Software_id")]
        public int SoftwareId { get; set; }

        [Column("User_id")]
        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public UserProfile? User { get; set; }
    }
}