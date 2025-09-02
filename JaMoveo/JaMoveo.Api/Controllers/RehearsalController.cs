using JaMoveo.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JaMoveo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RehearsalController : ControllerBase
    {
        private readonly IRehearsalService _rehearsalService;
        private readonly ILogger<RehearsalController> _logger;

        public RehearsalController(IRehearsalService rehearsalService, ILogger<RehearsalController> logger)
        {
            _rehearsalService = rehearsalService;
            _logger = logger;
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSession()
        {
            try
            {
                var userId = GetCurrentUserId();
                var session = await _rehearsalService.CreateSessionAsync(userId);

                _logger.LogInformation("New rehearsal room created by: {UserId}", userId);

                return Ok(session);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rehearsal room");
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveSession()
        {
            try
            {
                var session = await _rehearsalService.GetActiveSessionAsync();

                if (session == null)
                {
                    return NotFound(new { message = "No active rehearsal room available" });
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active rehearsal room");
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }

        [HttpPost("join/{sessionId}")]
        public async Task<IActionResult> JoinSession(string sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _rehearsalService.JoinSessionAsync(userId, sessionId);

                if (!success)
                {
                    return BadRequest(new { message = "Unable to join rehearsal room" });
                }

                _logger.LogInformation("User {UserId} joined rehearsal room {SessionId}", userId, sessionId);

                return Ok(new { message = "Successfully joined the rehearsal room" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining rehearsal room");
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }

        [HttpPost("leave/{sessionId}")]
        public async Task<IActionResult> LeaveSession(string sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _rehearsalService.LeaveSessionAsync(userId, sessionId);

                if (!success)
                {
                    return BadRequest(new { message = "Unable to leave rehearsal room" });
                }

                _logger.LogInformation("User {UserId} left rehearsal room {SessionId}", userId, sessionId);

                return Ok(new { message = "You have left the rehearsal room" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving rehearsal room");
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }

        [HttpPost("select-song/{songId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SelectSong(int songId)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var success = await _rehearsalService.SelectSongAsync(songId, adminId);

                if (!success)
                {
                    return BadRequest(new { message = "Unable to select the song" });
                }

                _logger.LogInformation("Admin {AdminId} selected song {SongId}", adminId, songId);

                return Ok(new { message = "Song selected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting song");
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }

        [HttpPost("end")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EndSession()
        {
            try
            {
                var adminId = GetCurrentUserId();
                var success = await _rehearsalService.EndSessionAsync(adminId);

                if (!success)
                {
                    return BadRequest(new { message = "Unable to end rehearsal room" });
                }

                _logger.LogInformation("Admin {AdminId} ended the rehearsal room", adminId);

                return Ok(new { message = "Rehearsal room ended" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending rehearsal room");
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }

        [HttpGet("current-song")]
        public async Task<IActionResult> GetCurrentSong()
        {
            try
            {
                var song = await _rehearsalService.GetCurrentSongAsync();

                if (song == null)
                {
                    return NotFound(new { message = "No active song available" });
                }

                return Ok(song);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current song");
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }

        [HttpGet("connected-users/{sessionId}")]
        public async Task<IActionResult> GetConnectedUsers(string sessionId)
        {
            try
            {
                var users = await _rehearsalService.GetConnectedUsersAsync(sessionId);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connected users");
                return StatusCode(500, new { message = "An internal error occurred" });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim.Value);
        }
    }
}
