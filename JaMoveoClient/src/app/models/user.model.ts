export interface User {
  id: number;
  username: string;
  role: string;
  instrument: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface SignupRequest {
  username: string;
  password: string;
  instrument: EInstrument;
}

export interface LoginResponse {
  token: string;
  user: User;
}

export enum EInstrument {
  Drums = 0,
  Guitar = 1,
  Bass = 2,
  Saxophone = 3,
  Keyboards = 4,
  Singers = 5
}
