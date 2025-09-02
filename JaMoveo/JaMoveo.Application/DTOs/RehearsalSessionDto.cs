namespace JaMoveo.Core.DTOs
{
    public class RehearsalSessionDto
    {
        public int Id { get; set; }
        public string SessionId { get; set; }
        public int AdminUserId { get; set; }
        public string AdminUsername { get; set; }
        public int? CurrentSongId { get; set; }
        public string CurrentSongName { get; set; }
        public string CurrentSongArtist { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> ConnectedUsers { get; set; } = new();
    }
}
