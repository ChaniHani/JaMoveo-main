using JaMoveo.Core.DTOs;
using JaMoveo.Core.Interfaces;
using JaMoveo.Infrastructure.Entities;
using JaMoveo.Infrastructure.Enums;
using System.Text.Json;

namespace JaMoveo.Core.Services
{
    public class RehearsalService : IRehearsalService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RehearsalService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<RehearsalSessionDto> CreateSessionAsync(int adminUserId)
        {
            var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId);
            if (admin == null || admin.Role != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("רק מנהלים יכולים ליצור חדר חזרות");
            }

            var existingSession = await _unitOfWork.RehearsalSessions.GetActiveSessionAsync();
            if (existingSession != null)
            {
                throw new InvalidOperationException("קיים כבר חדר חזרות פעיל");
            }

            var session = new RehearsalSession
            {
                AdminUserId = adminUserId,
                SessionId = Guid.NewGuid().ToString(),
                IsActive = true
            };

            await _unitOfWork.RehearsalSessions.CreateAsync(session);
            session = await _unitOfWork.RehearsalSessions.GetByIdAsync(session.Id);

            return MapToSessionDto(session);
        }

        public async Task<RehearsalSessionDto> GetActiveSessionAsync()
        {
            var session = await _unitOfWork.RehearsalSessions.GetActiveSessionAsync();
            return session != null ? MapToSessionDto(session) : null;
        }

        public async Task<bool> JoinSessionAsync(int userId, string sessionId)
        {
            var session = await _unitOfWork.RehearsalSessions.GetBySessionIdAsync(sessionId);
            if (session == null || !session.IsActive)
            {
                return false;
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var existingConnection = session.ConnectedUsers
                .FirstOrDefault(cu => cu.UserId == userId && cu.LeftAt == null);

            if (existingConnection == null)
            {
                var userSession = new UserRehearsalSession
                {
                    UserId = userId,
                    RehearsalSessionId = session.Id,
                    JoinedAt = DateTime.UtcNow
                };

                session.ConnectedUsers.Add(userSession);
                await _unitOfWork.RehearsalSessions.UpdateAsync(session);
            }

            return true;
        }

        public async Task<bool> LeaveSessionAsync(int userId, string sessionId)
        {
            var session = await _unitOfWork.RehearsalSessions.GetBySessionIdAsync(sessionId);
            if (session == null)
            {
                return false;
            }

            var userSession = session.ConnectedUsers
                .FirstOrDefault(cu => cu.UserId == userId && cu.LeftAt == null);

            if (userSession != null)
            {
                userSession.LeftAt = DateTime.UtcNow;
                await _unitOfWork.RehearsalSessions.UpdateAsync(session);
                return true;
            }

            return false;
        }

        public async Task<bool> SelectSongAsync(int songId, int adminUserId)
        {
            var session = await _unitOfWork.RehearsalSessions.GetActiveSessionAsync();
            if (session == null || session.AdminUserId != adminUserId)
            {
                return false;
            }

            var songExists = await _unitOfWork.Songs.ExistsAsync(songId);
            if (!songExists)
            {
                return false;
            }

            session.CurrentSongId = songId;
            await _unitOfWork.RehearsalSessions.UpdateAsync(session);

            return true;
        }

        public async Task<bool> EndSessionAsync(int adminUserId)
        {
            var session = await _unitOfWork.RehearsalSessions.GetActiveSessionAsync();
            if (session == null || session.AdminUserId != adminUserId)
            {
                return false;
            }

            return await _unitOfWork.RehearsalSessions.EndSessionAsync(session.Id, adminUserId);
        }

        public async Task<SongDto> GetCurrentSongAsync()
        {
            var session = await _unitOfWork.RehearsalSessions.GetActiveSessionAsync();
            if (session?.CurrentSong == null)
            {
                return null;
            }

            return new SongDto
            {
                Id = session.CurrentSong.Id,
                Name = session.CurrentSong.Name,
                Artist = session.CurrentSong.Artist,
                ImageUrl = session.CurrentSong.ImageUrl,
                Lines = JsonSerializer.Deserialize<List<List<WordChordPair>>>(session.CurrentSong.SongContentJson),
                Language = session.CurrentSong.Language
            };
        }


        public async Task<List<string>> GetConnectedUsersAsync(string sessionId)
        {
            var session = await _unitOfWork.RehearsalSessions.GetBySessionIdAsync(sessionId);
            if (session == null)
            {
                return new List<string>();
            }

            return session.ConnectedUsers
                .Where(cu => cu.LeftAt == null)
                .Select(cu => cu.User.UserName)
                .ToList();
        }

        private RehearsalSessionDto MapToSessionDto(RehearsalSession session)
        {
            return new RehearsalSessionDto
            {
                Id = session.Id,
                SessionId = session.SessionId,
                AdminUserId = session.AdminUserId,
                AdminUsername = session.Admin?.UserName,
                CurrentSongId = session.CurrentSongId,
                CurrentSongName = session.CurrentSong?.Name,
                CurrentSongArtist = session.CurrentSong?.Artist,
                IsActive = session.IsActive,
                CreatedAt = session.CreatedAt,
                ConnectedUsers = session.ConnectedUsers?
                    .Where(cu => cu.LeftAt == null)
                    .Select(cu => cu.User.UserName)
                    .ToList() ?? new List<string>()
            };
        }
    }
}