import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Song, SongSearchResponse, SongSearchResult } from '../models/song.model';
import { API_CONFIG } from '../config/api.config';


@Injectable({
  providedIn: 'root'
})
export class SongService {

  private readonly http = inject(HttpClient);

  searchSongs(query: string): Observable<SongSearchResponse> {
    return this.http.get<SongSearchResponse>(`${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.SONGS.SEARCH}?query=${encodeURIComponent(query)}&page=1`);
  }

  getSong(song: SongSearchResult): Observable<Song> {
    return this.http.post<Song>(`${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.SONGS.GET}`, song);
  }
}
