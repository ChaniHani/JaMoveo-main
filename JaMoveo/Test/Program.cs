// See https://aka.ms/new-console-template for more information
using JaMoveo.Application.Providers;
using JaMoveo.Infrastructure.Entities;

Console.WriteLine("Hello, World!");


var scraper = new Tab4UProvider();

try
{
    // Search for songs
    string searchTerm = "מעלות";
    var results = await scraper.FetchSongAsync(searchTerm, 1);

    Console.WriteLine($"Found {results.TotalResults} results for '{results.SearchTerm}'");
    Console.WriteLine($"Showing {results.Songs.Count} songs on this page");
    Console.WriteLine();

    foreach (var song in results.Songs)
    {
        Console.WriteLine($"Title: {song.Title}");
        Console.WriteLine($"Artist: {song.Artist}");
        Console.WriteLine($"URL: {song.Url}");
        Console.WriteLine($"Artist Image: {song.ImageUrl ?? "No image available"}");
        Console.WriteLine($"Song ID: {song.SongId}");
        Console.WriteLine($"Has Tabs: {song.HasTabs}");
        Console.WriteLine($"Has Notes: {song.HasNotes}");
        Console.WriteLine(new string('-', 50));
    }

    // Get next page if available
    if (results.HasNextPage)
    {
        Console.WriteLine("Getting next page...");
        var nextPageResults = await scraper.FetchSongAsync(searchTerm, 2);
        Console.WriteLine($"Next page has {nextPageResults.Songs.Count} more songs");
    }

    // Example: Get content of the first song
    if (results.Songs.Count > 0)
    {
        Console.WriteLine("\nFetching content of first song...");
        var firstSong = results.Songs[1];
        var songDetails = await scraper.GetSongDetailsAsync(firstSong.Url);
        Console.WriteLine($"Title: {songDetails.Title}");
        Console.WriteLine($"Artist: {songDetails.Artist}");
        Console.WriteLine($"Artists: {string.Join(", ", songDetails.Artists)}");
        Console.WriteLine($"Category: {songDetails.Category}");
        Console.WriteLine($"Song ID: {songDetails.SongId}");
        Console.WriteLine($"Total lines: {songDetails.Lines.Count}");


        // Show word-by-word format (first 10 pairs)
        Console.WriteLine("\nWord-by-word format:");
        foreach (var line in songDetails.Lines)
        {
            Console.WriteLine("Line: [");
            foreach (var pair in line)
            {
                if (!string.IsNullOrEmpty(pair.Chords))
                    Console.WriteLine($"  {{\"lyrics\": \"{pair.Lyrics}\", \"chords\": \"{pair.Chords}\"}}");
                else
                    Console.WriteLine($"  {{\"lyrics\": \"{pair.Lyrics}\"}}");
            }
            Console.WriteLine("]");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

