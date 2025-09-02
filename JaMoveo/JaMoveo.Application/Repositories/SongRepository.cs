using JaMoveo.Core.Interfaces;
using JaMoveo.Infrastructure.Data;
using JaMoveo.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace JaMoveo.Core.Repositories
{
    public class SongRepository : ISongRepository
    {
        private readonly ApplicationDbContext _context;

        public SongRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Song> GetByIdAsync(int id)
        {
            return await _context.Songs.FindAsync(id);
        }

        public async Task<Song> GetByProviderIdAsync(int id)
        {
            return await _context.Songs.FirstOrDefaultAsync(p => p.SongIdProvider == id);
        }

        public async Task<List<Song>> SearchAsync(string query)
        {
            //TODO: change
            return await _context.Songs
                .Where(s => s.Name.Contains(query) || s.Artist.Contains(query))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<Song>> GetAllAsync()
        {
            return await _context.Songs
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Song> CreateAsync(Song song)
        {
            _context.Songs.Add(song);
            await _context.SaveChangesAsync();
            return song;
        }

        public async Task<Song> UpdateAsync(Song song)
        {
            _context.Songs.Update(song);
            await _context.SaveChangesAsync();
            return song;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var song = await GetByIdAsync(id);
            if (song == null) return false;

            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Songs.AnyAsync(s => s.Id == id);
        }

        public async Task<List<Song>> GetByLanguageAsync(string language)
        {
            return await _context.Songs
                .Where(s => s.Language == language)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}