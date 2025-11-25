import { Injectable } from '@angular/core';
import { ComponentStore } from '@ngrx/component-store';
import { EMPTY, catchError, switchMap, tap } from 'rxjs';

import { UiArt } from '../core/models/art.model';
import { ArtService } from '../core/services/art.service';

interface ArtState {
  loadingArt: boolean;
  loadingFeatured: boolean;
  art: UiArt[];
  featured: UiArt[];
  error?: string;
}

@Injectable()
export class ArtStore extends ComponentStore<ArtState> {
  readonly art$ = this.select((state) => state.art);
  readonly featured$ = this.select((state) => state.featured);
  readonly loadingArt$ = this.select((state) => state.loadingArt);
  readonly loadingFeatured$ = this.select((state) => state.loadingFeatured);
  readonly error$ = this.select((state) => state.error);

  readonly loadArt = this.effect<void>((trigger$) =>
    trigger$.pipe(
      tap(() => this.patchState({ loadingArt: true, error: undefined })),
      switchMap(() =>
        this.artService.getAll().pipe(
          tap((art: UiArt[]) => this.patchState({ art, loadingArt: false })),
          catchError((error: unknown) => {
            const message = error instanceof Error ? error.message : 'Unknown error';
            this.patchState({ loadingArt: false, error: message });
            return EMPTY;
          })
        )
      )
    )
  );

  readonly loadFeatured = this.effect<void>((trigger$) =>
    trigger$.pipe(
      tap(() => this.patchState({ loadingFeatured: true, error: undefined })),
      switchMap(() =>
        this.artService.getFeatured(5).pipe(
          tap((featured: UiArt[]) => this.patchState({ featured, loadingFeatured: false })),
          catchError((error: unknown) => {
            const message = error instanceof Error ? error.message : 'Unknown error';
            this.patchState({ loadingFeatured: false, error: message });
            return EMPTY;
          })
        )
      )
    )
  );

  constructor(private readonly artService: ArtService) {
    super({ loadingArt: false, loadingFeatured: false, art: [], featured: [] });
  }
}
