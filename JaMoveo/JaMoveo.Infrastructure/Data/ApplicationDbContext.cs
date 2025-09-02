using JaMoveo.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JaMoveo.Infrastructure.Data;
/// <summary>
/// Provides a database context for the BookStore application using Entity Framework Core and Identity Framework.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    /// <summary>
    /// Initializes a new instance of the ApplicationDbContext class with the specified DbContext options.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }


    public DbSet<Song> Songs { get; set; }
    public DbSet<RehearsalSession> RehearsalSessions { get; set; }
    public DbSet<UserRehearsalSession> UserRehearsalSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Role).HasConversion<int>();
            entity.Property(u => u.Instrument).HasConversion<int>();

            entity.HasMany(u => u.AdminSessions)
                  .WithOne(rs => rs.Admin)
                  .HasForeignKey(rs => rs.AdminUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.UserSessions)
                  .WithOne(urs => urs.User)
                  .HasForeignKey(urs => urs.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });


        // Configure Song entity
        modelBuilder.Entity<Song>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Artist).IsRequired();
            entity.Property(s => s.Language).IsRequired().HasMaxLength(2);
            //entity.Property(s => s.Lyrics).IsRequired();

            entity.HasMany(s => s.RehearsalSessions)
                  .WithOne(rs => rs.CurrentSong)
                  .HasForeignKey(rs => rs.CurrentSongId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Song>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Artist).HasMaxLength(100);
            entity.Property(s => s.Language).HasMaxLength(2);
  

            entity.HasMany(s => s.RehearsalSessions)
                  .WithOne(rs => rs.CurrentSong)
                  .HasForeignKey(rs => rs.CurrentSongId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure RehearsalSession entity
        modelBuilder.Entity<RehearsalSession>(entity =>
        {
            entity.HasKey(rs => rs.Id);
            entity.HasIndex(rs => rs.SessionId).IsUnique();
            entity.Property(rs => rs.SessionId).IsRequired();

            entity.HasOne(rs => rs.Admin)
                  .WithMany(u => u.AdminSessions)
                  .HasForeignKey(rs => rs.AdminUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(rs => rs.CurrentSong)
                  .WithMany(s => s.RehearsalSessions)
                  .HasForeignKey(rs => rs.CurrentSongId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(rs => rs.ConnectedUsers)
                        .WithOne(urs => urs.RehearsalSession)
                        .HasForeignKey(urs => urs.RehearsalSessionId)
                        .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRehearsalSession>(entity =>
        {
            entity.HasKey(urs => urs.Id);

            entity.HasOne(urs => urs.User)
                  .WithMany(u => u.UserSessions)
                  .HasForeignKey(urs => urs.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(urs => urs.RehearsalSession)
                  .WithMany(rs => rs.ConnectedUsers)
                  .HasForeignKey(urs => urs.RehearsalSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Identity Tables names in Hebrew (optional)
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<IdentityRole<int>>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

        modelBuilder.Entity<IdentityRole<int>>().HasData(
                new IdentityRole<int> { Id = 1, Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole<int> { Id = 2, Name = "Player", NormalizedName = "PLAYER" }
            );

    }



}