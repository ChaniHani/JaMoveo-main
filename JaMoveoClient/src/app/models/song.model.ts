export interface Song {
  id: number;
  name: string;
  artist: string;
  imageUrl: string;
  language: string;
  lines: SongWord[][];}

export interface SongWord {
  lyrics: string;
  chords?: string;
}



export interface SongSearchResult {
  songId: number;
  title: string;
  artist: string;
  imageUrl: string;
  language: string;
  url: string;
}

export interface SongSearchResponse {
  songs: SongSearchResult[];
  totalResults: number;
  hasNextPage: number;
  nextPageUrl: number;
}

export interface RehearsalSession{
  sessionId: string;
}
