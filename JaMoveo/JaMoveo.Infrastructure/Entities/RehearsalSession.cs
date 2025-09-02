using System.ComponentModel.DataAnnotations;

namespace JaMoveo.Infrastructure.Entities
{
    public class RehearsalSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SessionId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int AdminUserId { get; set; }

        public virtual ApplicationUser Admin { get; set; }

        public int? CurrentSongId { get; set; }

        public virtual Song CurrentSong { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<UserRehearsalSession> ConnectedUsers { get; set; } = new List<UserRehearsalSession>();

    }
}
