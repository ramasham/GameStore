export interface Game {
  id: number;
  name: string;
  genre: string;
  price: number;
  releaseDate: string;
  imageUrl: string | null;
}

export interface GameDetails {
  id: number;
  name: string;
  genreId: number;
  price: number;
  releaseDate: string;
  imageUrl: string | null;
}

export interface GameMutation {
  name: string;
  genreId: number;
  price: number;
  releaseDate: string;
}

export interface ImageUploadResponse {
  imageUrl: string;
}

export interface Genre {
  id: number;
  name: string;
}
