using HtmlAgilityPack;
using JaMoveo.Application.Interfaces;
using JaMoveo.Core.DTOs;
using System.Text;
using System.Web;

namespace JaMoveo.Application.Providers
{


    public class Tab4USearchResponse
    {
        public List<SongResult> Songs { get; set; } = new List<SongResult>();
        public int TotalResults { get; set; }
        public string SearchTerm { get; set; }
        public bool HasNextPage { get; set; }
        public string NextPageUrl { get; set; }
    }

    public class Tab4UProvider : IExternalSongProvider
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://www.tab4u.com";


        public Tab4UProvider()
        {
            _httpClient = new HttpClient();
            // Set a proper User-Agent to avoid being blocked
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

        }
        public async Task<Tab4USearchResponse> FetchSongAsync(string query,int page)
        {
            try
            {
                // URL encode the Hebrew search term
                string encodedSearchTerm = HttpUtility.UrlEncode(query, Encoding.UTF8);

                // Calculate offset for pagination (30 results per page)
                int offset = (page - 1) * 30;

                string url = $"{BaseUrl}/resultsSimple?tab=songs&q={encodedSearchTerm}&s={offset}";

                string html = await _httpClient.GetStringAsync(url);
                return ParseSearchResults(html, query);
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        private Tab4USearchResponse ParseSearchResults(string html, string searchTerm)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var response = new Tab4USearchResponse
            {
                SearchTerm = searchTerm
            };

            // Extract total results count
            var resultsHeader = doc.DocumentNode
                .SelectSingleNode("//h1[@class='searchResultsH1']");

            if (resultsHeader != null)
            {
                string resultsText = resultsHeader.InnerText;
                // Extract number from "נמצאו 130 תוצאות לחיפוש"
                var match = System.Text.RegularExpressions.Regex.Match(resultsText, @"(\d+)");
                if (match.Success)
                {
                    response.TotalResults = int.Parse(match.Groups[1].Value);
                }
            }

            // Find all song rows
            var songRows = doc.DocumentNode
                .SelectNodes("//tr[contains(@class, 'odd') or not(contains(@class, 'adTD'))]//div[@class='recUpUnit ruSongUnit']");

            if (songRows != null)
            {
                foreach (var songRow in songRows)
                {
                    var song = ExtractSongInfo(songRow);
                    if (song != null)
                    {
                        response.Songs.Add(song);
                    }
                }
            }

            // Check for next page
            var nextPageLink = doc.DocumentNode
                .SelectSingleNode("//a[@class='nextPre h']");

            if (nextPageLink != null)
            {
                response.HasNextPage = true;
                response.NextPageUrl = BaseUrl + nextPageLink.GetAttributeValue("href", "");
            }

            return response;
        }

        private SongResult ExtractSongInfo(HtmlNode songNode)
        {
            try
            {
                var linkNode = songNode.SelectSingleNode(".//a[@class='ruSongLink songLinkT focB userBackS h firstTopSL1']");
                if (linkNode == null) return null;

                var song = new SongResult();

                // Extract URL and song ID
                string href = linkNode.GetAttributeValue("href", "");
                if (!string.IsNullOrEmpty(href))
                {
                    song.Url = href.StartsWith("http") ? href : BaseUrl + "/" + href.TrimStart('/');

                    // Extract song ID from href (e.g., "tabs/songs/74920_...")
                    var idMatch = System.Text.RegularExpressions.Regex.Match(href, @"songs/(\d+)_");
                    if (idMatch.Success)
                    {
                        song.SongId = int.Parse(idMatch.Groups[1].Value);
                    }
                }

                // Extract song title and artist
                var titleNode = linkNode.SelectSingleNode(".//div[@class='sNameI19']");
                var artistNode = linkNode.SelectSingleNode(".//div[@class='aNameI19']");

                if (titleNode != null)
                {
                    song.Title = titleNode.InnerText.Trim().TrimEnd('/').Trim();
                }

                if (artistNode != null)
                {
                    song.Artist = artistNode.InnerText.Trim();
                }

                var artistImageNode = linkNode.SelectSingleNode(".//span[@class='ruArtPhoto relZ']");
                if (artistImageNode != null)
                {
                    var style = artistImageNode.GetAttributeValue("style", "");
                    if (!string.IsNullOrEmpty(style))
                    {
                        // Extract URL from background-image:url(...)
                        var imageMatch = System.Text.RegularExpressions.Regex.Match(style, @"background-image:url\(([^)]+)\)");
                        if (imageMatch.Success)
                        {
                            string imageUrl = imageMatch.Groups[1].Value.Trim();

                            // Handle relative URLs
                            if (imageUrl.StartsWith("/"))
                            {
                                song.ImageUrl = BaseUrl + imageUrl;
                            }
                            else if (!imageUrl.StartsWith("http"))
                            {
                                song.ImageUrl = BaseUrl + "/" + imageUrl;
                            }
                            else
                            {
                                song.ImageUrl = imageUrl;
                            }

                            // Check if it's the default "no artist pic" image
                            if (imageUrl.Contains("noArtPicDu.svg"))
                            {
                                song.ImageUrl = ""; 
                            }
                        }
                    }
                }



                // Check for tabs and notes availability (look in parent row)
                var parentRow = songNode.Ancestors("tr").FirstOrDefault();
                if (parentRow != null)
                {
                    var tabsLink = parentRow.SelectSingleNode(".//img[@alt='טאבים']");
                    var notesLink = parentRow.SelectSingleNode(".//img[@alt='תווים']");

                    song.HasTabs = tabsLink != null;
                    song.HasNotes = notesLink != null;
                }

                return song;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<SongContent> GetSongDetailsAsync(string songUrl)
        {
            try
            {
                string fullUrl = songUrl.StartsWith("http") ? songUrl : BaseUrl + "/" + songUrl.TrimStart('/');
                string html = await _httpClient.GetStringAsync(fullUrl);

                return Tab4USongParser.ParseSongPage(html, fullUrl);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
