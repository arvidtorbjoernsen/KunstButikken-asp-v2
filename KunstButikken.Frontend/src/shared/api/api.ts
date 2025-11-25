'use client';

/**
 * API service client that routes all requests through the AuthGateway
 * Similar to Angular's service architecture
 *
 * All API calls go through the gateway at /api/{service}/{path}
 * The gateway handles JWT validation and forwards to backend services
 */

import { parseResponse } from '@/shared/api/response';
import { buildServiceUrl } from '@/shared/config';

type Service = 'AUTH' | 'USER' | 'ART' | 'AUCTION' | 'PAYMENT' | 'ADMIN';

/**
 * Make an API call through the AuthGateway
 * Automatically attaches Bearer token from Keycloak
 */
export async function apiFetch<T = unknown>(
  service: Service,
  path: string,
  options: RequestInit = {},
  withAuth: boolean = true,
): Promise<T> {
  // Build full URL through the centralized config
  const url = buildServiceUrl(service, path);

  const headers = new Headers(options.headers as HeadersInit);
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json');
  }

  // Attach Authorization from Keycloak if available and requested
  if (withAuth && !headers.has('Authorization')) {
    try {
      // Dynamically import to avoid SSR issues
      const { getKeycloakToken } = await import('@/features/auth/lib/keycloak-client');
      const token = getKeycloakToken();
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
        if (process.env.NODE_ENV !== 'production') {
          console.debug(`[apiFetch] Token attached to ${service} request:`, url);
        }
      } else if (process.env.NODE_ENV !== 'production') {
        console.warn(`[apiFetch] No token available for ${service} request:`, url);
      }
    } catch (e) {
      // Keycloak might not be initialized yet; ignore
      if (process.env.NODE_ENV !== 'production') {
        console.warn('[apiFetch] Failed to get Keycloak token:', e);
      }
    }
  }

  const res = await fetch(url, {
    ...options,
    headers,
    credentials: withAuth ? 'include' : 'same-origin',
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`API error (${res.status}): ${text}`);
  }
  return parseResponse<T>(res);
}
