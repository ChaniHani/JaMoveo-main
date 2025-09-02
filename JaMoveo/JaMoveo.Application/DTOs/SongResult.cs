namespace JaMoveo.Core.DTOs
{
    public class SongResult
    {
        public string ImageUrl { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Url { get; set; }
        public bool HasTabs { get; set; }
        public bool HasNotes { get; set; }
        public int SongId { get; set; }
    }
}
