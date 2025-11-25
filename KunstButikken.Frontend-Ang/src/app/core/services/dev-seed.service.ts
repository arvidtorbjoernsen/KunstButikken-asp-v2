import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

interface SeedKeycloakResult {
  allowed: boolean;
  realm: string;
  created: string[];
  skipped: string[];
  errors: string[];
}

interface SyncKeycloakResult {
  allowed: boolean;
  triggered: boolean;
  lastRunError?: string;
  lastRunSucceeded?: boolean;
  lastRunUtc?: string;
  lastRunDuration?: string;
}

interface SeedArtResult {
  inserted: number;
  skipped: number;
  messages: string[];
}

interface BootstrapResult {
  ok: boolean;
  keycloak?: SeedKeycloakResult | { error: string };
  usersdb?: SyncKeycloakResult | { error: string };
  user?: { status: number; ok: boolean; data: unknown } | { error: string };
  art?: SeedArtResult | { error: string };
}

@Injectable({
  providedIn: 'root'
})
export class DevSeedService {

  private get apiBase() {
    return (environment.apis?.gateway || '').replace(/\/$/, '');
  }

  constructor(private http: HttpClient) {
  }

  /**
   * Seeds demo users in Keycloak
   * Creates: seller1-3, buyer1-7, app-admin
   */
  seedKeycloakUsers(force = false): Observable<SeedKeycloakResult> {
    const endpoint = `${this.apiBase}/api/dev/seed-keycloak-users`;
    return this.http.post<SeedKeycloakResult>(endpoint, { force });
  }

  /**
   * Syncs Keycloak users to UserService database
   */
  syncKeycloakUsers(): Observable<SyncKeycloakResult> {
    const endpoint = `${this.apiBase}/api/dev/sync-keycloak-users`;
    return this.http.post<SyncKeycloakResult>(endpoint, {});
  }

  /**
   * Seeds art items from SeedImages directory
   */
  seedArt(limit = 10, locale = 'en', force = false): Observable<SeedArtResult> {
    const endpoint = `${this.apiBase}/api/seed`;
    return this.http.post<SeedArtResult>(endpoint, { limit, locale, force });
  }

  /**
   * Bootstrap everything: users + art
   * This is the equivalent of curl http://localhost:3000/api/dev/bootstrap
   */
  bootstrapAll(): Observable<BootstrapResult> {
    // We'll call the endpoints in sequence
    return new Observable(observer => {
      const result: BootstrapResult = { ok: true };

      // Step 1: Seed Keycloak users
      this.seedKeycloakUsers(false).subscribe({
        next: (keycloakResult) => {
          result.keycloak = keycloakResult;

          // Step 2: Sync users to database
          this.syncKeycloakUsers().subscribe({
            next: (syncResult) => {
              result.usersdb = syncResult;

              // Step 3: Seed art
              this.seedArt(10, 'en', false).subscribe({
                next: (artResult) => {
                  result.art = artResult;
                  observer.next(result);
                  observer.complete();
                },
                error: (err) => {
                  result.art = { error: err.message || 'Failed to seed art' };
                  result.ok = false;
                  observer.next(result);
                  observer.complete();
                }
              });
            },
            error: (err) => {
              result.usersdb = { error: err.message || 'Failed to sync users' };
              result.ok = false;
              observer.next(result);
              observer.complete();
            }
          });
        },
        error: (err) => {
          result.keycloak = { error: err.message || 'Failed to seed Keycloak users' };
          result.ok = false;
          observer.next(result);
          observer.complete();
        }
      });
    });
  }

  /**
   * Get current user profile (creates one if it doesn't exist)
   */
  ensureCurrentUserProfile(): Observable<unknown> {
    const endpoint = `${this.apiBase}/api/profile/me`;
    return this.http.get(endpoint);
  }
}
