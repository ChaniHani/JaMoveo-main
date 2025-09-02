using JaMoveo.Application.Interfaces;
using JaMoveo.Core.Interfaces;
using JaMoveo.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace JaMoveo.Application.Hubs;

[Authorize]
public class RehearsalHub : Hub
{
    private readonly IRehearsalService _rehearsalService;
    private readonly ISongService _songService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RehearsalHub> _logger;

    public RehearsalHub(IRehearsalService rehearsalService, ISongService songService,
                        UserManager<ApplicationUser> userManager, ILogger<RehearsalHub> logger)
    {
        _rehearsalService = rehearsalService;
        _songService = songService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task JoinRehearsal()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(userId.ToString());
            var username = user?.UserName ?? "Unknown";
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            _logger.LogInformation("User {Username} is trying to join rehearsal room (Admin: {IsAdmin})", username, isAdmin);

            // Find the active rehearsal session
            var activeSession = await _rehearsalService.GetActiveSessionAsync();

            // If there is no active session and user is admin - create a new session
            if (activeSession == null && isAdmin)
            {
                _logger.LogInformation("No active session, creating a new session for admin {Username}", username);
                activeSession = await _rehearsalService.CreateSessionAsync(userId);

                await Groups.AddToGroupAsync(Context.ConnectionId, "rehearsal");
                await Clients.Caller.SendAsync("SessionCreated", activeSession);

                _logger.LogInformation("New rehearsal session created successfully: {SessionId}", activeSession.SessionId);
                return;
            }

            if (activeSession != null)
            {
                await _rehearsalService.JoinSessionAsync(userId, activeSession.SessionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, "rehearsal");

                _logger.LogInformation("User {Username} joined the rehearsal room", username);

                // Notify all users about the new participant
                await Clients.Group("rehearsal").SendAsync("UserJoined", username);

                // Send session details to the new user
                await Clients.Caller.SendAsync("JoinedSession", activeSession);

                // If there is an active song, send it to the new user
                if (activeSession.CurrentSongId.HasValue)
                {
                    var currentSong = await _songService.GetSongByIdAsync(activeSession.CurrentSongId.Value);
                    await Clients.Caller.SendAsync("SongSelected", currentSong);
                }
            }
            else
            {
                _logger.LogWarning("No active rehearsal session and user {Username} is not an admin", username);
                await Clients.Caller.SendAsync("NoActiveSession");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while joining rehearsal room");
            await Clients.Caller.SendAsync("Error", "Error while joining rehearsal room");
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task CreateNewSession()
    {
        try
        {
            var adminId = GetCurrentUserId();

            // End existing session if any
            var existingSession = await _rehearsalService.GetActiveSessionAsync();
            if (existingSession != null)
            {
                await _rehearsalService.EndSessionAsync(adminId);
                await Clients.Group("rehearsal").SendAsync("SessionEnded");
            }

            // Create a new session
            var newSession = await _rehearsalService.CreateSessionAsync(adminId);

            _logger.LogInformation("Admin {AdminId} created a new rehearsal session: {SessionId}", adminId, newSession.SessionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, "rehearsal");
            await Clients.Caller.SendAsync("SessionCreated", newSession);

            // Notify all users about the new session
            await Clients.All.SendAsync("NewSessionAvailable", "A new rehearsal session has started!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating a new rehearsal session");
            await Clients.Caller.SendAsync("Error", "Error while creating a new rehearsal session");
        }
    }

    public async Task LeaveRehearsal()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(userId.ToString());
            var username = user?.UserName ?? "Unknown";

            var activeSession = await _rehearsalService.GetActiveSessionAsync();
            if (activeSession != null)
            {
                await _rehearsalService.LeaveSessionAsync(userId, activeSession.SessionId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "rehearsal");

                _logger.LogInformation("User {Username} left the rehearsal room", username);

                await Clients.Group("rehearsal").SendAsync("UserLeft", username);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while leaving rehearsal room");
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task SelectSong(int songId)
    {
        try
        {
            var adminId = GetCurrentUserId();
            var success = await _rehearsalService.SelectSongAsync(songId, adminId);

            if (success)
            {
                var song = await _songService.GetSongByIdAsync(songId);

                _logger.LogInformation("Admin selected song: {SongName} - {Artist}", song.Name, song.Artist);

                // Send the selected song to all connected users
                await Clients.Group("rehearsal").SendAsync("SongSelected", song);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Unable to select the song");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting song: {SongId}", songId);
            await Clients.Caller.SendAsync("Error", "Error selecting the song");
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task QuitSession()
    {
        try
        {
            var adminId = GetCurrentUserId();
            var success = await _rehearsalService.EndSessionAsync(adminId);

            if (success)
            {
                _logger.LogInformation("Admin ended the rehearsal session");

                // Notify all users that the session ended
                await Clients.Group("rehearsal").SendAsync("SessionEnded");
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Unable to end the session");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while ending rehearsal session");
            await Clients.Caller.SendAsync("Error", "Error while ending the session");
        }
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(userId.ToString());
            var username = user?.UserName ?? "Unknown";

            var activeSession = await _rehearsalService.GetActiveSessionAsync();
            if (activeSession != null)
            {
                await _rehearsalService.LeaveSessionAsync(userId, activeSession.SessionId);
            }

            await Clients.Group("rehearsal").SendAsync("UserLeft", username);

            _logger.LogInformation("User {Username} disconnected", username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disconnecting user");
        }

        await base.OnDisconnectedAsync(exception);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = Context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            throw new UnauthorizedAccessException("User not identified");
        }
        return userId;
    }
}