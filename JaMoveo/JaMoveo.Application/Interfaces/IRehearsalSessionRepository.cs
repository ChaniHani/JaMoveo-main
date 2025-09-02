using JaMoveo.Infrastructure.Entities;

namespace JaMoveo.Core.Interfaces
{
    public interface IRehearsalSessionRepository
    {
        Task<RehearsalSession> GetByIdAsync(int id);
        Task<RehearsalSession> GetBySessionIdAsync(string sessionId);
        Task<RehearsalSession> GetActiveSessionAsync();
        Task<RehearsalSession> CreateAsync(RehearsalSession session);
        Task<RehearsalSession> UpdateAsync(RehearsalSession session);
        Task<bool> EndSessionAsync(int sessionId, int adminUserId);
        Task<List<RehearsalSession>> GetUserSessionsAsync(int userId);
        Task<List<RehearsalSession>> GetAdminSessionsAsync(int adminUserId);
    }
}
