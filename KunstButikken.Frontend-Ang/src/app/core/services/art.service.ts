import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { sampleArt } from '../../sample/sample-art';
import { ApiArt, UiArt } from '../models/art.model';

const generateId = (): string => {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return Math.random().toString(36).slice(2);
};

@Injectable({ providedIn: 'root' })
export class ArtService {
  constructor(private readonly http: HttpClient) {
  }

  private get apiBase() {
    return (environment.apis.gateway || '').replace(/\/$/, '');
  }

  getFeatured(limit = 5) {
    const endpoint = this.apiBase ? `${this.apiBase}/api/art/featured?limit=${limit}` : '';
    if (!endpoint) {
      return of(sampleArt.slice(0, limit));
    }

    return this.http.get<ApiArt[]>(endpoint).pipe(
      map((data) => this.mapCollection(data).slice(0, limit)),
      catchError((error) => {
        console.warn('[ArtService] Falling back to sample data:', error);
        return of(sampleArt.slice(0, limit));
      })
    );
  }

  getAll() {
    const endpoint = this.apiBase ? `${this.apiBase}/api/art` : '';
    if (!endpoint) {
      return of(sampleArt);
    }

    return this.http.get<ApiArt[]>(endpoint).pipe(
      map((data) => this.mapCollection(data)),
      catchError((error) => {
        console.warn('[ArtService] Falling back to sample data:', error);
        return of(sampleArt);
      })
    );
  }

  getById(id?: string | number) {
    const endpoint = this.apiBase ? `${this.apiBase}/api/art/${id}` : '';
    if (!endpoint) {
      const found = sampleArt.find((a) => String(a.id) === String(id));
      return of(found);
    }

    return this.http.get<ApiArt>(endpoint).pipe(
      map((item) => this.mapArt(item)),
      catchError((error) => {
        console.warn('[ArtService] getById fallback to sample data:', error);
        const found = sampleArt.find((a) => String(a.id) === String(id));
        return of(found);
      })
    );
  }

  private mapCollection(data?: ApiArt[] | null): UiArt[] {
    if (!Array.isArray(data)) {
      return [];
    }
    return data.map((item) => this.mapArt(item));
  }

  private mapArt(item: ApiArt): UiArt {
    return {
      id: String(item.id ?? generateId()),
      titleNb: item.titleNb ?? item.titleEn ?? 'Uten tittel',
      titleEn: item.titleEn ?? item.titleNb ?? 'Untitled',
      descriptionNb: item.descriptionNb ?? undefined,
      descriptionEn: item.descriptionEn ?? undefined,
      artist: item.artist ?? item.sellerDisplayName ?? item.sellerId ?? 'Unknown Artist',
      sellerDisplayName: item.sellerDisplayName ?? undefined,
      price: item.price ?? 0,
      image: item.imageUrl ?? 'assets/images/placeholder.png'
    };
  }
}
