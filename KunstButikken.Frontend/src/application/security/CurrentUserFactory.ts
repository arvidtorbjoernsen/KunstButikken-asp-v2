import { parseRolesFromBearer } from '@/features/auth/lib/server-auth';
import type { CurrentUserContext } from './types';

export function createCurrentUserFromToken(token: string | null | undefined): CurrentUserContext {
  if (!token) {
    return { roles: [] };
  }

  try {
    const [, payload] = token.split('.');
    const decoded = payload ? JSON.parse(Buffer.from(payload, 'base64url').toString('utf8')) : {};
    const roles = parseRolesFromBearer(token);
    return {
      id: decoded?.sub,
      roles,
      token,
    };
  } catch {
    return { roles: [] };
  }
}
