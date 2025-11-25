/**
 * Client-side Keycloak token helper
 * Used by api.ts to attach tokens to requests
 */

// Store the token getter function (set by KeycloakProvider)
let tokenGetter: (() => string | undefined) | null = null;

export function setKeycloakClientTokenGetter(getter: () => string | undefined) {
  tokenGetter = getter;
}

export function getKeycloakToken(): string | undefined {
  return tokenGetter?.();
}
