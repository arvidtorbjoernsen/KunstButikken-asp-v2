/**
 * Server-side environment configuration
 * Similar to Angular's environment.ts, this provides a centralized configuration
 * that works consistently on both server and client (with proper checks)
 */

export interface Environment {
  production: boolean;
  appName: string;
  frontend: {
    origin: string;
    selfUrl: string;
  };
  stripe: {
    publishableKey: string;
  };
  apis: {
    // All API calls go through the AuthGateway (like Angular)
    gateway: string;
  };
  keycloak: {
    baseUrl: string;
    realm: string;
    issuer: string;
    clientId: string;
  };
}

/**
 * Get server-side environment configuration
 * This function reads from process.env and should only be called server-side
 */
export function getServerEnvironment(): Environment {
  // Ensure this runs only on the server
  if (typeof window !== 'undefined') {
    throw new Error('getServerEnvironment() should only be called server-side');
  }

  const keycloakBaseUrl =
    process.env.KEYCLOAK_BASE_URL ||
    process.env.NEXT_PUBLIC_KEYCLOAK_BASE_URL ||
    'http://localhost:8080';
  const keycloakRealm = process.env.KEYCLOAK_REALM || 'kunstbutikken';

  return {
    production: process.env.NODE_ENV === 'production',
    appName: 'KunstButikken',
    frontend: {
      origin:
        process.env.NEXTAUTH_URL || process.env.NEXT_PUBLIC_FRONTEND_URL || 'http://localhost:3000',
      selfUrl:
        process.env.NEXTAUTH_URL || process.env.NEXT_PUBLIC_FRONTEND_URL || 'http://localhost:3000',
    },
    stripe: {
      publishableKey: process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY || '',
    },
    apis: {
      // All API calls go through the AuthGateway (like Angular)
      gateway: process.env.NEXT_PUBLIC_API_GATEWAY || 'http://localhost:5000',
    },
    keycloak: {
      baseUrl: keycloakBaseUrl,
      realm: keycloakRealm,
      issuer:
        process.env.NEXT_PUBLIC_KEYCLOAK_ISSUER || `${keycloakBaseUrl}/realms/${keycloakRealm}`,
      clientId: process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || 'kunstbutikken-frontend',
    },
  };
}

/**
 * Get client-side environment configuration (safe subset)
 * This only includes environment variables prefixed with NEXT_PUBLIC_
 */
export function getClientEnvironment(): Partial<Environment> {
  const isBrowser = typeof window !== 'undefined';

  if (!isBrowser) {
    // If called server-side, return empty to avoid leaking secrets
    return {};
  }

  return {
    production: process.env.NODE_ENV === 'production',
    appName: 'KunstButikken',
    frontend: {
      origin: process.env.NEXT_PUBLIC_FRONTEND_URL || 'http://localhost:3000',
      selfUrl: process.env.NEXT_PUBLIC_FRONTEND_URL || 'http://localhost:3000',
    },
    stripe: {
      publishableKey: process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY || '',
    },
    apis: {
      // All API calls go through the AuthGateway (like Angular)
      gateway: process.env.NEXT_PUBLIC_API_GATEWAY || 'http://localhost:5000',
    },
    keycloak: {
      // Note: No hardcoded fallback for baseUrl - Aspire must provide the dynamic port
      baseUrl: process.env.NEXT_PUBLIC_KEYCLOAK_BASE_URL || '',
      realm: process.env.NEXT_PUBLIC_KEYCLOAK_REALM || 'kunstbutikken',
      issuer: process.env.NEXT_PUBLIC_KEYCLOAK_ISSUER || '',
      clientId: process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || 'kunstbutikken-frontend',
    },
  };
}
