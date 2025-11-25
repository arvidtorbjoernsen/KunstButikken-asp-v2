import { isPlatformBrowser } from '@angular/common';
import { inject, Injectable, InjectionToken, PLATFORM_ID } from '@angular/core';
import type Keycloak from 'keycloak-js';

// Custom token to expose Keycloak instance
export const KEYCLOAK = new InjectionToken<Keycloak>('KEYCLOAK');

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private keycloak = inject(KEYCLOAK, { optional: true });
  private platformId = inject(PLATFORM_ID);
  private isBrowser = isPlatformBrowser(this.platformId);

  getToken(): string | undefined {
    if (!this.isBrowser || !this.keycloak) {
      return undefined;
    }
    return this.keycloak.token;
  }

  getUsername(): string | undefined {
    if (!this.isBrowser || !this.keycloak) {
      return undefined;
    }
    return this.keycloak.tokenParsed?.['preferred_username'];
  }

  isLoggedIn(): boolean {
    if (!this.isBrowser || !this.keycloak) {
      return false;
    }
    return !!this.keycloak.token;
  }

  login(): Promise<void> {
    if (!this.isBrowser || !this.keycloak) {
      return Promise.resolve();
    }
    return this.keycloak.login({
      redirectUri: typeof window !== 'undefined' ? window.location.origin : undefined,
    });
  }

  logout(): void {
    if (!this.isBrowser || !this.keycloak) {
      return;
    }
    this.keycloak.logout({
      redirectUri: typeof window !== 'undefined' ? window.location.origin : undefined,
    });
  }
}
