/**
 * API Client with Authentication
 * Similar to Angular's HTTP interceptor that adds auth tokens
 *
 * This utility automatically attaches Keycloak tokens to API requests
 * going to the gateway, similar to Angular's authTokenInterceptor
 */

import { parseResponse } from './response';
import { getGatewayBase } from '@/shared/config';

let keycloakTokenGetter: (() => string | undefined) | null = null;
let keycloakTokenRefresher: (() => Promise<boolean>) | null = null;

/**
 * Set the token getter function (called from KeycloakProvider)
 */
export function setKeycloakTokenGetter(getter: (() => string | undefined) | null) {
  keycloakTokenGetter = getter;
}

export function setKeycloakTokenRefresher(refresher: (() => Promise<boolean>) | null) {
  keycloakTokenRefresher = refresher;
}

async function performFetch(
  url: string,
  options: RequestInit = {},
  isRetry = false,
): Promise<Response> {
  const gatewayUrl = getGatewayBase();
  const isApiRequest = url.startsWith(gatewayUrl) || url.startsWith('/api/');
  const headers = new Headers(options.headers);

  if (isApiRequest && keycloakTokenGetter) {
    const token = keycloakTokenGetter();
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
      console.debug('[apiFetch] Token attached to request:', url);
    } else {
      console.warn('[apiFetch] No token available for API request:', url);
    }
  } else {
    console.debug('[apiFetch] Skipping token for non-API request:', url);
  }

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (!isRetry && (response.status === 401 || response.status === 403) && keycloakTokenRefresher) {
    console.warn('[apiFetch] Received auth error, attempting token refresh...');
    const refreshed = await keycloakTokenRefresher();
    if (refreshed) {
      console.info('[apiFetch] Token refresh succeeded, retrying request');
      return performFetch(url, options, true);
    }
    console.warn('[apiFetch] Token refresh failed or unavailable');
  }

  return response;
}

/**
 * Convenience methods for common HTTP verbs
 * Similar to Angular's HttpClient methods
 */
export const apiClient = {
  async get<T = unknown>(url: string, options?: RequestInit): Promise<T> {
    const response = await apiFetch(url, { ...options, method: 'GET' });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    return parseResponse<T>(response);
  },

  async post<T = unknown>(url: string, body?: unknown, options?: RequestInit): Promise<T> {
    const response = await apiFetch(url, {
      ...options,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
      body: body ? JSON.stringify(body as unknown) : undefined,
    });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    return parseResponse<T>(response);
  },

  async put<T = unknown>(url: string, body?: unknown, options?: RequestInit): Promise<T> {
    const response = await apiFetch(url, {
      ...options,
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
      body: body ? JSON.stringify(body as unknown) : undefined,
    });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    return parseResponse<T>(response);
  },

  async delete<T = unknown>(url: string, options?: RequestInit): Promise<T> {
    const response = await apiFetch(url, { ...options, method: 'DELETE' });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    return parseResponse<T>(response);
  },

  async patch<T = unknown>(url: string, body?: unknown, options?: RequestInit): Promise<T> {
    const response = await apiFetch(url, {
      ...options,
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
      body: body ? JSON.stringify(body as unknown) : undefined,
    });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    return parseResponse<T>(response);
  },
};

export async function apiFetch(url: string, options: RequestInit = {}): Promise<Response> {
  return performFetch(url, options);
}
