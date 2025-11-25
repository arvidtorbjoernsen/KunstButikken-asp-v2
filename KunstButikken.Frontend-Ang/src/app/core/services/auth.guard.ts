import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { KeycloakService } from './keycloak.service';

// Modern Angular 20 functional guard
export const AuthGuard: CanActivateFn = async (route, state) => {
  const keycloakService = inject(KeycloakService);
  const router = inject(Router);

  console.log('[AuthGuard] Checking authentication...');

  try {
    const isLoggedIn = keycloakService.isLoggedIn();
    console.log('[AuthGuard] User logged in:', isLoggedIn);

    if (!isLoggedIn) {
      console.log('[AuthGuard] User not logged in - redirecting to Keycloak login');
      // Store the intended URL for redirect after login
      const returnUrl = state.url;
      // Redirect to Keycloak login
      keycloakService.login();
      return false;
    }

    return true;
  } catch (error) {
    console.error('[AuthGuard] Error checking authentication:', error);
    return router.createUrlTree(['/']);
  }
};
