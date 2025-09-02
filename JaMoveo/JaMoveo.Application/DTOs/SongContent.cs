namespace JaMoveo.Core.DTOs
{
    public class SongContent
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public List<string> Artists { get; set; } = new List<string>();
        public string ChordsAndLyrics { get; set; }
        public string Category { get; set; }
        public int SongId { get; set; }
        public List<List<WordChordPair>> Lines { get; set; } = new List<List<WordChordPair>>();

    }
}
