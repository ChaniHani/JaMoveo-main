using System.ComponentModel.DataAnnotations;

namespace JaMoveo.Infrastructure.Entities
{
    public class UserRehearsalSession
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int RehearsalSessionId { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LeftAt { get; set; }

        // Navigation properties
        public virtual ApplicationUser User { get; set; }
        public virtual RehearsalSession RehearsalSession { get; set; }
    }
}
