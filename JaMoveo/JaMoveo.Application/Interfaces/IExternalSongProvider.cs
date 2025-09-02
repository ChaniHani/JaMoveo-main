using JaMoveo.Application.Providers;
using JaMoveo.Core.DTOs;

namespace JaMoveo.Application.Interfaces
{
    public interface IExternalSongProvider
    {
        Task<Tab4USearchResponse> FetchSongAsync(string query, int page);
        Task<SongContent> GetSongDetailsAsync(string songUrl);
    }
}
