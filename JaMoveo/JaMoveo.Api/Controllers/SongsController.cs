using JaMoveo.Application.Interfaces;
using JaMoveo.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JaMoveo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SongsController : ControllerBase
    {
        private readonly ISongService _songService;
        private readonly ILogger<SongsController> _logger;

        public SongsController(ISongService songService, ILogger<SongsController> logger)
        {
            _songService = songService;
            _logger = logger;
        }

        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchSongs([FromQuery] string query, [FromQuery] int page)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { message = "Search term is required" });
                }

                var songs = await _songService.SearchSongsAsync(query, page);

                _logger.LogInformation("Songs search performed for: {Query}, {Count} results found", query, songs.TotalResults);

                return Ok(songs);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching songs for: {Query}", query);
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }

        [HttpPost("GetSong")]
        public async Task<IActionResult> GetSong(SongResult songResult)
        {
            try
            {
                var song = await _songService.GetSongByProviderAsync(songResult);

                _logger.LogInformation("Song loaded successfully: {SongId} - {SongName}", songResult.SongId, song.Name);

                return Ok(song);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Song not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading song: {SongId}", songResult.SongId);
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }
    }
}
