using JaMoveo.Infrastructure.Entities;

namespace JaMoveo.Core.Interfaces
{
    public interface ISongRepository
    {
        Task<Song> GetByIdAsync(int id);
        Task<Song> GetByProviderIdAsync(int id);
        Task<List<Song>> SearchAsync(string query);
        Task<List<Song>> GetAllAsync();
        Task<Song> CreateAsync(Song song);
        Task<Song> UpdateAsync(Song song);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<List<Song>> GetByLanguageAsync(string language);
    }
}
