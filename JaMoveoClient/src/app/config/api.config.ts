import { environment } from '../../environments/environment';

export const API_CONFIG = {
  BASE_URL: environment.apiUrl,
  SIGNALR_URL: environment.signalRUrl,
  ENDPOINTS: {
    AUTH: {
      LOGIN: 'Auth/login',
      SIGNUP: 'Auth/signup',
      SIGNUP_ADMIN: 'Auth/signup-admin'
    },
    SONGS: {
      SEARCH: 'Songs/search',
      GET: 'Songs/GetSong'
    }
  }
};
