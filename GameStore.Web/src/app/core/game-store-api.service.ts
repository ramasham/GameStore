import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Game, GameDetails, GameMutation, Genre, ImageUploadResponse } from './models';

@Injectable({ providedIn: 'root' })
export class GameStoreApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  getGames(): Observable<Game[]> {
    return this.http.get<Game[]>(`${this.baseUrl}/games`);
  }

  getGameDetails(id: number): Observable<GameDetails> {
    return this.http.get<GameDetails>(`${this.baseUrl}/games/${id}`);
  }

  createGame(payload: GameMutation): Observable<GameDetails> {
    return this.http.post<GameDetails>(`${this.baseUrl}/games`, payload);
  }

  updateGame(id: number, payload: GameMutation): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/games/${id}`, payload);
  }

  deleteGame(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/games/${id}`);
  }

  uploadGameImage(id: number, file: File): Observable<ImageUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<ImageUploadResponse>(`${this.baseUrl}/games/${id}/image`, formData);
  }

  getGenres(): Observable<Genre[]> {
    return this.http.get<Genre[]>(`${this.baseUrl}/genres`);
  }
}
