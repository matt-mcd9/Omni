using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omni.Models
{
    [Table("Game")]
    public class Game
    {
        [Key, Column("Game_id")]
        public int GameId { get; set; }

        [Column("User_id")]
        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public UserProfile? User { get; set; }
    }
}