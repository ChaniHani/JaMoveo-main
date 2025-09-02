using HtmlAgilityPack;
using JaMoveo.Core.DTOs;

namespace JaMoveo.Application.Providers
{
    public static class Tab4USongParser
    {
        public static SongContent ParseSongPage(string html, string url = "")
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var songContent = new SongContent();

            // Extract song ID from URL if provided
            if (!string.IsNullOrEmpty(url))
            {
                var idMatch = System.Text.RegularExpressions.Regex.Match(url, @"songs/(\d+)_");
                if (idMatch.Success)
                {
                    songContent.SongId = int.Parse(idMatch.Groups[1].Value);
                }
            }

            // Extract song title - look for h1 with song title pattern
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1[contains(text(), 'אקורדים לשיר')]");
            if (titleNode != null)
            {
                var titleText = titleNode.InnerText;
                // Extract title from "אקורדים לשיר [TITLE] של [ARTIST]"
                var titleMatch = System.Text.RegularExpressions.Regex.Match(titleText, @"אקורדים לשיר\s+(.+?)\s+של");
                if (titleMatch.Success)
                {
                    songContent.Title = titleMatch.Groups[1].Value.Trim();
                }
            }

            // Extract artist names
            var artistLinks = doc.DocumentNode.SelectNodes("//a[@class='artistTitle rightKotInSong hT']");
            if (artistLinks != null)
            {
                foreach (var artistLink in artistLinks)
                {
                    var artistName = artistLink.InnerText.Trim();
                    if (!string.IsNullOrEmpty(artistName))
                    {
                        songContent.Artists.Add(artistName);
                    }
                }
                songContent.Artist = string.Join(" ו", songContent.Artists);
            }

            // Extract category
            var categoryLink = doc.DocumentNode.SelectSingleNode("//a[@class='catLinkInSong hT']");
            if (categoryLink != null)
            {
                songContent.Category = categoryLink.InnerText.Trim();
            }

            // Extract main song content
            var contentNode = doc.DocumentNode.SelectSingleNode("//div[@id='songContentTPL']");
            if (contentNode != null)
            {
                ParseContentToWordChordPairs(contentNode, songContent);
            }

            return songContent;
        }




        private static void ParseContentToWordChordPairs(HtmlNode contentNode, SongContent songContent)
        {
            var tables = contentNode.SelectNodes(".//table");
            if (tables == null) return;

            foreach (var table in tables)
            {
                var rows = table.SelectNodes(".//tr");
                if (rows == null) continue;

                string pendingChords = "";

                foreach (var row in rows)
                {
                    var chordCell = row.SelectSingleNode(".//td[@class='chords']");
                    var lyricCell = row.SelectSingleNode(".//td[@class='song']");

                    // Check for section headers like "סיום:" in titLine spans
                    var sectionHeader = row.SelectSingleNode(".//span[@class='titLine']");
                    if (sectionHeader != null)
                    {
                        // This is a section header, we can optionally add it as a line
                        var headerText = sectionHeader.InnerText.Trim();
                        if (!string.IsNullOrEmpty(headerText))
                        {
                            var headerLine = new List<WordChordPair> { new WordChordPair($"[{headerText}]") };
                            songContent.Lines.Add(headerLine);
                        }
                        continue;
                    }

                    if (chordCell != null)
                    {
                        // Get chords with preserved spacing
                        pendingChords = chordCell.InnerText?.Replace("&nbsp;", " ") ?? "";
                    }
                    else if (lyricCell != null)
                    {
                        // Get lyrics with preserved spacing
                        var lyricsText = lyricCell.InnerText?.Replace("&nbsp;", " ") ?? "";

                        if (!string.IsNullOrEmpty(lyricsText.Trim()))
                        {
                            // Create word-chord pairs for this line
                            var linePairs = CreateWordChordPairs(pendingChords, lyricsText);
                            if (linePairs.Count > 0)
                            {
                                songContent.Lines.Add(linePairs);
                            }
                        }

                        pendingChords = "";
                    }
                }
            }
        }

        private static List<WordChordPair> CreateWordChordPairs(string chordLine, string lyricsLine)
        {
            var pairs = new List<WordChordPair>();

            if (string.IsNullOrEmpty(lyricsLine?.Trim()))
                return pairs;

            // Extract chords from chord line
            var chords = ExtractChords(chordLine);

            // Split lyrics into words
            var words = lyricsLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Match chords to words
            int chordIndex = 0;
            foreach (var word in words)
            {
                string assignedChord = null;

                if (chordIndex < chords.Count)
                {
                    assignedChord = chords[chordIndex];
                    chordIndex++;
                }

                pairs.Add(new WordChordPair(word.Trim(), assignedChord));
            }

            return pairs;
        }

        private static List<string> ExtractChords(string chordText)
        {
            var chords = new List<string>();

            if (string.IsNullOrEmpty(chordText))
                return chords;

            // Find chord patterns
            var chordPattern = @"([A-G][#b]?(?:maj|min|m|dim|aug|sus|add)?[0-9]*(?:/[A-G][#b]?)?)";
            var matches = System.Text.RegularExpressions.Regex.Matches(chordText, chordPattern);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Success && !string.IsNullOrEmpty(match.Value.Trim()))
                {
                    chords.Add(match.Value.Trim());
                }
            }

            return chords;
        }
       
    }
}
