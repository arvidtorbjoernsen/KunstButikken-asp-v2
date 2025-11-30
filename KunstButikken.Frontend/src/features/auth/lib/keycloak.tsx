'use client';

import type Keycloak from 'keycloak-js';
import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { setKeycloakTokenGetter, setKeycloakTokenRefresher } from '@/shared/api/api-client';
import { setKeycloakClientTokenGetter } from './keycloak-client';

/**
 * Keycloak Service for Next.js
 * Similar to Angular's KeycloakService but using React patterns
 *
 * This service:
 * - Initializes Keycloak on the client side only
 * - Uses check-sso for silent authentication
 * - Provides authentication state via React context
 * - Handles token refresh automatically
 */

interface KeycloakContextValue {
  keycloak: Keycloak | null;
  authenticated: boolean;
  loading: boolean;
  login: () => void;
  logout: () => void;
  getToken: () => string | undefined;
  getUsername: () => string | undefined;
  isLoggedIn: () => boolean;
  hasRole: (role: string) => boolean;
  isAdmin: boolean;
  isSeller: boolean;
  isBuyer: boolean;
}

const KeycloakContext = createContext<KeycloakContextValue>({
  keycloak: null,
  authenticated: false,
  loading: true,
  login: () => {},
  logout: () => {},
  getToken: () => undefined,
  getUsername: () => undefined,
  isLoggedIn: () => false,
  hasRole: () => false,
  isAdmin: false,
  isSeller: false,
  isBuyer: false,
});

export const useKeycloak = () => useContext(KeycloakContext);

export const hasRole = (keycloak: Keycloak | null, role: string) =>
  keycloak?.tokenParsed?.realm_access?.roles?.includes(role) ?? false;

interface KeycloakProviderProps {
  children: React.ReactNode;
}

/**
 * Keycloak Provider Component
 * Must be used client-side only (has "use client" directive)
 * Similar to Angular's APP_INITIALIZER for Keycloak
 */
export function KeycloakProvider({ children }: KeycloakProviderProps) {
  const [keycloak, setKeycloak] = useState<Keycloak | null>(null);
  const [authenticated, setAuthenticated] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Only run in browser
    if (typeof window === 'undefined') {
      return;
    }

    console.log('[KeycloakProvider] Initializing Keycloak...');

    // Get configuration from environment (similar to Angular's environment.ts)
    // Note: No hardcoded fallback for baseUrl - Aspire must provide the dynamic port
    const keycloakBaseUrl = process.env.NEXT_PUBLIC_KEYCLOAK_BASE_URL || '';
    const keycloakRealm = process.env.NEXT_PUBLIC_KEYCLOAK_REALM || 'kunstbutikken';
    const keycloakClientId = process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || 'kunstbutikken-frontend';
    const frontendUrl = process.env.NEXT_PUBLIC_FRONTEND_URL || window.location.origin;

    console.log('[KeycloakProvider] Config:', {
      baseUrl: keycloakBaseUrl,
      realm: keycloakRealm,
      clientId: keycloakClientId,
      frontendUrl: frontendUrl,
    });

    if (!keycloakBaseUrl || !keycloakRealm || !keycloakClientId) {
      console.error('[KeycloakProvider] Missing Keycloak configuration - skipping init');
      setLoading(false);
      return;
    }

    // Resolve issuer/discovery URL and perform a fast sanity check so the UI doesn't hang
    const issuer = process.env.NEXT_PUBLIC_KEYCLOAK_ISSUER || `${keycloakBaseUrl}/realms/${keycloakRealm}`;
    const discoveryUrl = `${issuer.replace(/\/$/, '')}/.well-known/openid-configuration`;
    console.log('[KeycloakProvider] Resolved Keycloak issuer:', issuer);
    console.log('[KeycloakProvider] Discovery URL:', discoveryUrl);

    // Small helper to fetch discovery with timeout
    const checkDiscovery = async (url: string, timeoutMs = 3000) => {
      try {
        const controller = new AbortController();
        const id = setTimeout(() => controller.abort(), timeoutMs);
        const res = await fetch(url, { method: 'GET', signal: controller.signal });
        clearTimeout(id);
        if (!res.ok) {
          throw new Error(`OIDC discovery returned ${res.status}`);
        }
        return true;
      } catch (err) {
        console.error('[KeycloakProvider] OIDC discovery check failed:', err);
        return false;
      }
    };

    // Fail fast if OIDC discovery is unreachable. This prevents long waiting states during init.
    checkDiscovery(discoveryUrl, 3000)
      .then((ok) => {
        if (!ok) {
          console.error('[KeycloakProvider] Keycloak OIDC discovery unreachable; skipping Keycloak init to avoid hanging.');
          setLoading(false);
          return;
        }

        // Dynamically import keycloak-js (client-side only)
        import('keycloak-js')
          .then(({ default: Keycloak }) => {
            console.log('[KeycloakProvider] Creating Keycloak instance...');

            const keycloakInstance = new Keycloak({
              url: keycloakBaseUrl,
              realm: keycloakRealm,
              clientId: keycloakClientId,
            });

            console.log('[KeycloakProvider] Initializing Keycloak with check-sso...');

            keycloakInstance
              .init({
                onLoad: 'check-sso',
                checkLoginIframe: false, // Disable iframe-based SSO check to prevent timeout issues
                enableLogging: process.env.NODE_ENV !== 'production',
              })
              .then((auth: boolean) => {
                console.log('[KeycloakProvider] Keycloak initialized successfully!');
                console.log('[KeycloakProvider] User authenticated:', auth);
                setKeycloak(keycloakInstance);
                setAuthenticated(auth);
                setLoading(false);

                // Register callbacks (similar to Angular's registerCallbacks)
                keycloakInstance.onAuthSuccess = () => {
                  console.log('[KeycloakProvider] Auth success');
                  setAuthenticated(true);
                };

                keycloakInstance.onAuthLogout = () => {
                  console.log('[KeycloakProvider] Auth logout');
                  setAuthenticated(false);
                };

                keycloakInstance.onTokenExpired = () => {
                  console.log('[KeycloakProvider] Token expired, refreshing...');
                  keycloakInstance
                    .updateToken(30)
                    .then((refreshed: boolean) => {
                      if (refreshed) {
                        console.log('[KeycloakProvider] Token refreshed successfully');
                      } else {
                        console.log('[KeycloakProvider] Token still valid');
                      }
                    })
                    .catch(() => {
                      console.warn('[KeycloakProvider] Token refresh failed, forcing logout');
                      setAuthenticated(false);
                    });
                };
              })
              .catch((error: unknown) => {
                console.error('[KeycloakProvider] Failed to initialize Keycloak:', error);
                setLoading(false);
              });
          })
          .catch((err) => {
            console.error('[KeycloakProvider] Failed to import keycloak-js module:', err);
            setLoading(false);
          });
      })
      .catch((err) => {
        console.error('[KeycloakProvider] Failed to check OIDC discovery:', err);
        setLoading(false);
      });
  }, []);

  const login = () => {
    if (!keycloak) {
      console.warn('[KeycloakProvider] Keycloak not initialized');
      return;
    }
    const redirectUri =
      process.env.NEXT_PUBLIC_FRONTEND_URL ||
      (typeof window !== 'undefined' ? window.location.origin : undefined);
    keycloak.login({ redirectUri });
  };

  const logout = () => {
    if (!keycloak) {
      console.warn('[KeycloakProvider] Keycloak not initialized');
      return;
    }
    const redirectUri =
      process.env.NEXT_PUBLIC_FRONTEND_URL ||
      (typeof window !== 'undefined' ? window.location.origin : undefined);
    setAuthenticated(false);
    keycloak.logout({ redirectUri });
  };

  const getToken = (): string | undefined => {
    const token = keycloak?.token;
    console.log('[KeycloakProvider] getToken called, token exists:', !!token);
    if (token) {
      console.log('[KeycloakProvider] Token (first 50 chars):', token.substring(0, 50) + '...');
    } else {
      console.warn('[KeycloakProvider] NO TOKEN AVAILABLE');
      console.log('[KeycloakProvider] Keycloak instance exists:', !!keycloak);
      console.log('[KeycloakProvider] Authenticated state:', authenticated);
    }
    return token;
  };

  // Register token getter for API client (similar to Angular's interceptor)
  useEffect(() => {
    if (keycloak) {
      setKeycloakTokenGetter(getToken);
      setKeycloakClientTokenGetter(getToken);
      setKeycloakTokenRefresher(async () => {
        if (!keycloak) {
          return false;
        }
        try {
          const refreshed = await keycloak.updateToken(30);
          if (refreshed) {
            console.log('[KeycloakProvider] Token refreshed via api-client');
          }
          return true;
        } catch (err) {
          console.warn('[KeycloakProvider] Token refresh failed:', err);
          setAuthenticated(false);
          return false;
        }
      });
    }
    return () => {
      setKeycloakTokenRefresher(() => Promise.resolve(false));
    };
  }, [keycloak]);

  const getUsername = (): string | undefined => {
    return keycloak?.tokenParsed?.['preferred_username'];
  };

  const isLoggedIn = (): boolean => {
    return !!keycloak?.token;
  };

  const hasRole = useMemo(
    () =>
      (role: string): boolean => {
        if (!keycloak?.tokenParsed) {
          return false;
        }
        const roleLower = role.toLowerCase();
        const realmRoles = keycloak.tokenParsed?.['realm_access']?.['roles'] || [];
        if (realmRoles.some((r: string) => r.toLowerCase() === roleLower)) {
          return true;
        }
        const resourceAccess = keycloak.tokenParsed?.['resource_access'];
        if (resourceAccess) {
          for (const client in resourceAccess) {
            const clientRoles = resourceAccess[client]?.['roles'] || [];
            if (clientRoles.some((r: string) => r.toLowerCase() === roleLower)) {
              return true;
            }
          }
        }
        return false;
      },
    [keycloak, authenticated],
  );

  const isAdmin = useMemo(() => hasRole('admin'), [hasRole]);
  const isSeller = useMemo(() => hasRole('seller'), [hasRole]);
  const isBuyer = useMemo(() => hasRole('buyer'), [hasRole]);

  const value: KeycloakContextValue = {
    keycloak,
    authenticated,
    loading,
    login,
    logout,
    getToken,
    getUsername,
    isLoggedIn,
    hasRole,
    isAdmin,
    isSeller,
    isBuyer,
  };

  return <KeycloakContext.Provider value={value}>{children}</KeycloakContext.Provider>;
}
