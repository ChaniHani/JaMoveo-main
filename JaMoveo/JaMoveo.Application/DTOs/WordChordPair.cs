namespace JaMoveo.Core.DTOs
{
    public class WordChordPair
    {
        public string Lyrics { get; set; }
        public string Chords { get; set; }

        public WordChordPair(string lyrics, string chords = null)
        {
            this.Lyrics = lyrics;
            this.Chords = chords;
        }
    }

}
