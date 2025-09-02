using JaMoveo.Core.DTOs;

namespace JaMoveo.Core.Interfaces
{
    public interface IRehearsalService
    {
        Task<RehearsalSessionDto> CreateSessionAsync(int adminUserId);
        Task<RehearsalSessionDto> GetActiveSessionAsync();
        Task<bool> JoinSessionAsync(int userId, string sessionId);
        Task<bool> LeaveSessionAsync(int userId, string sessionId);
        Task<bool> SelectSongAsync(int songId, int adminUserId);
        Task<bool> EndSessionAsync(int adminUserId);
        Task<SongDto> GetCurrentSongAsync();
        Task<List<string>> GetConnectedUsersAsync(string sessionId);
    }
}
