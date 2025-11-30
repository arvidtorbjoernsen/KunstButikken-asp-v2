import type { CurrentUserContext } from './types';

type EnsureRoleOptions = {
  allowGuests?: boolean;
  errorFactory?: () => Error;
};

export function ensureRole(
  user: CurrentUserContext,
  requiredRoles: string[],
  options: EnsureRoleOptions = {},
): void {
  const { allowGuests = false, errorFactory } = options;

  if (!user || !user.roles || user.roles.length === 0) {
    if (allowGuests) return;
    throw errorFactory?.() ?? new Error('Unauthorized: missing role context');
  }

  const normalizedRequired = requiredRoles.map(role => role.toLowerCase());
  const normalizedUserRoles = user.roles.map(role => role.toLowerCase());

  const hasRole = normalizedRequired.some(role => normalizedUserRoles.includes(role));
  if (!hasRole && !allowGuests) {
    throw errorFactory?.() ?? new Error(`Forbidden: requires one of [${requiredRoles.join(', ')}]`);
  }
}

