import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { finalize, forkJoin } from 'rxjs';

import { GameStoreApiService } from './core/game-store-api.service';
import { Game, Genre } from './core/models';

interface CartEntry {
  game: Game;
  quantity: number;
  lineTotal: number;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  private readonly api = inject(GameStoreApiService);

  readonly games = signal<Game[]>([]);
  readonly genres = signal<Genre[]>([]);
  readonly cartItems = signal<Record<number, number>>({});
  readonly wishlistIds = signal<number[]>([]);
  readonly isLoading = signal(true);
  readonly message = signal<string | null>(null);
  readonly query = signal('');
  readonly isCartPopupOpen = signal(false);
  readonly isWishlistPopupOpen = signal(false);

  readonly gameCount = computed(() => this.games().length);
  readonly cartCount = computed(() =>
    Object.values(this.cartItems()).reduce((sum, quantity) => sum + quantity, 0)
  );
  readonly wishlistCount = computed(() => this.wishlistIds().length);

  readonly filteredGames = computed(() => {
    const query = this.query().trim().toLowerCase();
    const catalog = this.games().filter((game) => !!game.imageUrl);

    if (query.length === 0) {
      return catalog;
    }

    return catalog.filter(
      (game) => game.name.toLowerCase().includes(query) || game.genre.toLowerCase().includes(query)
    );
  });

  readonly cartEntries = computed<CartEntry[]>(() => {
    const gamesById = new Map(this.games().map((game) => [game.id, game]));

    return Object.entries(this.cartItems())
      .map(([gameIdText, quantity]) => {
        const gameId = Number(gameIdText);
        const game = gamesById.get(gameId);

        if (!game || quantity <= 0) {
          return null;
        }

        return {
          game,
          quantity,
          lineTotal: Number(game.price) * quantity
        } as CartEntry;
      })
      .filter((entry): entry is CartEntry => entry !== null)
      .sort((a, b) => a.game.name.localeCompare(b.game.name));
  });

  readonly cartTotal = computed(() =>
    this.cartEntries().reduce((sum, entry) => sum + entry.lineTotal, 0)
  );

  readonly wishlistGames = computed(() => {
    const ids = new Set(this.wishlistIds());
    return this.games()
      .filter((game) => ids.has(game.id) && !!game.imageUrl)
      .sort((a, b) => a.name.localeCompare(b.name));
  });

  ngOnInit(): void {
    this.loadInitialData();
  }

  onQueryChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.query.set(input.value);
  }

  openCartPopup(): void {
    this.isCartPopupOpen.set(true);
    this.isWishlistPopupOpen.set(false);
  }

  openWishlistPopup(): void {
    this.isWishlistPopupOpen.set(true);
    this.isCartPopupOpen.set(false);
  }

  closePopups(): void {
    this.isCartPopupOpen.set(false);
    this.isWishlistPopupOpen.set(false);
  }

  addToCart(game: Game): void {
    this.cartItems.update((items) => ({
      ...items,
      [game.id]: (items[game.id] ?? 0) + 1
    }));
    this.showMessage(`Added "${game.name}" to cart.`);
  }

  decreaseFromCart(gameId: number): void {
    this.cartItems.update((items) => {
      const current = items[gameId] ?? 0;
      const next = { ...items };

      if (current <= 1) {
        delete next[gameId];
      } else {
        next[gameId] = current - 1;
      }

      return next;
    });
  }

  removeFromCart(gameId: number): void {
    this.cartItems.update((items) => {
      const next = { ...items };
      delete next[gameId];
      return next;
    });
  }

  clearCart(): void {
    this.cartItems.set({});
  }

  toggleWishlist(gameId: number): void {
    this.wishlistIds.update((ids) =>
      ids.includes(gameId) ? ids.filter((id) => id !== gameId) : [...ids, gameId]
    );
  }

  removeFromWishlist(gameId: number): void {
    this.wishlistIds.update((ids) => ids.filter((id) => id !== gameId));
  }

  moveWishlistToCart(game: Game): void {
    this.addToCart(game);
    this.removeFromWishlist(game.id);
  }

  clearWishlist(): void {
    this.wishlistIds.set([]);
  }

  getCoverImage(game: Game): string {
    const cacheKey = game.imageUrl ?? 'generated';
    return `/api/games/${game.id}/cover?v=${encodeURIComponent(cacheKey)}`;
  }

  cartQuantity(gameId: number): number {
    return this.cartItems()[gameId] ?? 0;
  }

  isInWishlist(gameId: number): boolean {
    return this.wishlistIds().includes(gameId);
  }

  trackByGameId(_: number, game: Game): number {
    return game.id;
  }

  trackByCartEntry(_: number, entry: CartEntry): number {
    return entry.game.id;
  }

  private loadInitialData(): void {
    this.isLoading.set(true);

    forkJoin({
      games: this.api.getGames(),
      genres: this.api.getGenres()
    })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: ({ games, genres }) => {
          this.games.set(games);
          this.genres.set(genres);
        },
        error: () => {
          this.showMessage('Could not load store data from API.');
        }
      });
  }

  private showMessage(text: string): void {
    this.message.set(text);

    globalThis.setTimeout(() => {
      if (this.message() === text) {
        this.message.set(null);
      }
    }, 2200);
  }
}
