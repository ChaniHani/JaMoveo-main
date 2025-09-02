using JaMoveo.Core.Interfaces;
using JaMoveo.Infrastructure.Data;
using JaMoveo.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace JaMoveo.Core.Repositories
{
    public class RehearsalSessionRepository : IRehearsalSessionRepository
    {
        private readonly ApplicationDbContext _context;

        public RehearsalSessionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RehearsalSession> GetByIdAsync(int id)
        {
            return await _context.RehearsalSessions
                .Include(rs => rs.Admin)
                .Include(rs => rs.CurrentSong)
                .Include(rs => rs.ConnectedUsers)
                    .ThenInclude(cu => cu.User)
                .FirstOrDefaultAsync(rs => rs.Id == id);
        }

        public async Task<RehearsalSession> GetBySessionIdAsync(string sessionId)
        {
            return await _context.RehearsalSessions
                .Include(rs => rs.Admin)
                .Include(rs => rs.CurrentSong)
                .Include(rs => rs.ConnectedUsers)
                    .ThenInclude(cu => cu.User)
                .FirstOrDefaultAsync(rs => rs.SessionId == sessionId);
        }

        public async Task<RehearsalSession> GetActiveSessionAsync()
        {
            return await _context.RehearsalSessions
                .Include(rs => rs.Admin)
                .Include(rs => rs.CurrentSong)
                .Include(rs => rs.ConnectedUsers)
                    .ThenInclude(cu => cu.User)
                .FirstOrDefaultAsync(rs => rs.IsActive);
        }

        public async Task<RehearsalSession> CreateAsync(RehearsalSession session)
        {
            _context.RehearsalSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<RehearsalSession> UpdateAsync(RehearsalSession session)
        {
            _context.RehearsalSessions.Update(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<bool> EndSessionAsync(int sessionId, int adminUserId)
        {
            var session = await _context.RehearsalSessions
                .FirstOrDefaultAsync(rs => rs.Id == sessionId && rs.AdminUserId == adminUserId);

            if (session == null) return false;

            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RehearsalSession>> GetUserSessionsAsync(int userId)
        {
            return await _context.RehearsalSessions
                .Include(rs => rs.Admin)
                .Include(rs => rs.CurrentSong)
                .Where(rs => rs.ConnectedUsers.Any(cu => cu.UserId == userId))
                .OrderByDescending(rs => rs.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<RehearsalSession>> GetAdminSessionsAsync(int adminUserId)
        {
            return await _context.RehearsalSessions
                .Include(rs => rs.CurrentSong)
                .Include(rs => rs.ConnectedUsers)
                    .ThenInclude(cu => cu.User)
                .Where(rs => rs.AdminUserId == adminUserId)
                .OrderByDescending(rs => rs.CreatedAt)
                .ToListAsync();
        }
    }
}