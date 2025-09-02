using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JaMoveo.Infrastructure.Entities
{
    public class Song
    {
        [Key]
        public int Id { get; set; }

        public int SongIdProvider { get; set; }

        [Required]
        public string Name { get; set; }

        public string Artist { get; set; }
        [StringLength(500)]
        public string ImageUrl { get; set; }

        [StringLength(500)]
        public string SongUrlProvider { get; set; }

        [StringLength(2)]
        public string Language { get; set; } // "en" or "he"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(max)")]
        public string SongContentJson { get; set; }

        // Navigation properties
        public virtual ICollection<RehearsalSession> RehearsalSessions { get; set; } = new List<RehearsalSession>();

    }
}
