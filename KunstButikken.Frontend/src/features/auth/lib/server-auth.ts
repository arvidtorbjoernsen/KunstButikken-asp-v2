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
    const [, payload] = token.split('.');
    if (!payload) return [];
    const decoded = JSON.parse(Buffer.from(payload, 'base64url').toString('utf8')) as {
      realm_access?: { roles?: string[] };
      resource_access?: Record<string, { roles?: string[] }>;
    };
    const realmRoles: string[] = decoded?.realm_access?.roles ?? [];
    const clientRoles: string[] = Object.values(decoded?.resource_access ?? {})
      .flatMap(r => r.roles ?? []);
    return [...new Set([...realmRoles, ...clientRoles])];
  } catch {
    return [];
  }
}

export function hasRole(roles: string[], target: string): boolean {
  return roles.map(r => r.toLowerCase()).includes(target.toLowerCase());
}
