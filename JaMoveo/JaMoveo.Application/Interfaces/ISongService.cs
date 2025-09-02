using JaMoveo.Application.Providers;
using JaMoveo.Core.DTOs;

namespace JaMoveo.Application.Interfaces
{
    public interface ISongService
    {
        Task<Tab4USearchResponse> SearchSongsAsync(string query, int page);
        Task<SongDto> GetSongByIdAsync(int id );
        Task<SongDto> GetSongByProviderAsync(SongResult songResult);
    }
}
