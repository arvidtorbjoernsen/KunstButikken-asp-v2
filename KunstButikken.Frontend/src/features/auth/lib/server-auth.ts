import jwt from 'jsonwebtoken';

/**
 * Small server-side authentication helpers for Next.js route handlers.
 *
 * These are intentionally conservative: they only perform basic checks
 * (presence and format of Bearer token) and a development bypass flag.
 * Full token introspection/validation should be implemented in the backend
 * or using a secured service account when available.
 */

export function isDevBypass(req: Request): boolean {
  try {
    return process.env.NODE_ENV !== 'production' && req.headers.get('x-dev-admin') === '1';
  } catch {
    return false;
  }
}

export function getBearerToken(req: Request): string | null {
  try {
    const header = req.headers.get('authorization') || req.headers.get('Authorization');
    if (!header) return null;
    const m = header.match(/^Bearer\s+(.+)$/i);
    return m ? m[1] : null;
  } catch {
    return null;
  }
}

export function parseRolesFromBearer(token: string | null): string[] {
  if (!token) return [];
  try {
    const decoded = jwt.decode(token, { json: true }) as { realm_access?: { roles?: string[] } } | null;
    return decoded?.realm_access?.roles ?? [];
  } catch {
    return [];
  }
}

export function hasRole(roles: string[], role: string): boolean {
  return roles.includes(role);
}
