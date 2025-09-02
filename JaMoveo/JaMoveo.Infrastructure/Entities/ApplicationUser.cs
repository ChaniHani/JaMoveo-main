using JaMoveo.Infrastructure.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace JaMoveo.Infrastructure.Entities;

/// <summary>
/// ApplicationUser class will inherit the class IdentityUser so that we can add a field Name to User's Identity table in database
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    [Required]
    public UserRole Role { get; set; }
    [Required]
    public EInstrument Instrument { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
    // Navigation properties
    public virtual ICollection<RehearsalSession> AdminSessions { get; set; } = new List<RehearsalSession>();
    public virtual ICollection<UserRehearsalSession> UserSessions { get; set; } = new List<UserRehearsalSession>();
}
