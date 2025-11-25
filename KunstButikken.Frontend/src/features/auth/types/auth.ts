// Cross-cutting auth-related types shared across multiple features/routes
// NextAuth was removed in this workspace; define a minimal local session shape
// that matches what we expect from Keycloak-sourced session objects.

export type LocalSession = {
	user?: {
		name?: string;
		email?: string;
		sub?: string;
		roles?: string[];
		isAdmin?: boolean;
		isSeller?: boolean;
		isBuyer?: boolean;
		[k: string]: unknown;
	};
	expires?: string;
	accessToken?: string;
	idToken?: string;
};

// Minimal shape we rely on when checking authorization in route handlers
export type SessionUser = { roles?: string[]; isAdmin?: boolean };

// Loose type for debug tools where role array may not be strongly typed
export type UserWithRoles = { roles?: unknown };
