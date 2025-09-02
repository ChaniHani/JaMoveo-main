using JaMoveo.Application.Interfaces;
using JaMoveo.Application.Providers;
using JaMoveo.Core.DTOs;
using JaMoveo.Core.Interfaces;
using JaMoveo.Infrastructure.Entities;
using System.Text.Json;

namespace JaMoveo.Core.Services
{
    public class SongService : ISongService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IExternalSongProvider _externalSongProvider;


        public SongService(IUnitOfWork unitOfWork, IExternalSongProvider externalSongProvider)
        {
            _unitOfWork = unitOfWork;
            _externalSongProvider = externalSongProvider;
        }


        public async Task<Tab4USearchResponse> SearchSongsAsync(string query, int page)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("חובה להזין מילת חיפוש");
            }

            var res = await _externalSongProvider.FetchSongAsync(query, page);

            return res;
        }

        public async Task<SongDto> GetSongByIdAsync(int id)
        {
            var song = await _unitOfWork.Songs.GetByIdAsync(id);

            if (song == null)
            {
                throw new ArgumentException("");
            }

            return MapToSongDto(song);
        }

        public async Task<SongDto> GetSongByProviderAsync(SongResult songResult)
        {
            var song = await _unitOfWork.Songs.GetByProviderIdAsync(songResult.SongId);

            if (song == null)
            {
                SongContent songDetails = await _externalSongProvider.GetSongDetailsAsync(songResult.Url);
                var mapSong = MapToSong(songDetails, songResult);
                song = await _unitOfWork.Songs.CreateAsync(mapSong);
            }

            return MapToSongDto(song);
        }

        public async Task<List<SongSearchResultDto>> GetAllSongsAsync()
        {
            var songs = await _unitOfWork.Songs.GetAllAsync();

            return songs.Select(MapToSearchResultDto).ToList();
        }

        public async Task<bool> SongExistsAsync(int songId)
        {
            return await _unitOfWork.Songs.ExistsAsync(songId);
        }

        private Song MapToSong(SongContent song, SongResult songResult)
        {
            return new Song
            {
                SongUrlProvider = songResult.Url,
                ImageUrl = songResult.ImageUrl,
                SongIdProvider = song.SongId,
                Name = songResult.Title,
                Artist = song.Artist,
                SongContentJson= JsonSerializer.Serialize(song.Lines),
                Language = DetectLanguage(songResult.Title)
            };
        }

        private SongDto MapToSongDto(Song song)
        {
            List<List<WordChordPair>> list = new List<List<WordChordPair>>();
            if (!string.IsNullOrEmpty(song.SongContentJson))
            {
                try
                {
                    list = JsonSerializer.Deserialize<List<List<WordChordPair>>>(song.SongContentJson);
                }
                catch (JsonException ex)
                {
                }
            }

            return new SongDto
            {
                Id = song.Id,
                Name = song.Name,
                Artist = song.Artist,
                ImageUrl = song.ImageUrl,
                Lines = list,
                Language = song.Language
            };
        }

        private SongSearchResultDto MapToSearchResultDto(Song song)
        {
            return new SongSearchResultDto
            {
                Id = song.Id,
                Name = song.Name,
                Artist = song.Artist,
                ImageUrl = song.ImageUrl,
                Language = DetectLanguage(song.Name)
            };
        }

        private string DetectLanguage(string text)
        {
            // Simple Hebrew detection
            if (text.Any(c => c >= 0x0590 && c <= 0x05FF))
            {
                return "he";
            }
            return "en";
        }
    }
}
