// Shared Keycloak token parsed types for the frontend

export type KeycloakResourceAccess = {
  [client: string]: {
    roles?: string[];
  };
};

export type KeycloakTokenParsed = {
  exp?: number;
  iat?: number;
  jti?: string;
  iss?: string;
  aud?: string | string[];
  sub?: string;
  typ?: string;
  azp?: string;
  nonce?: string;
  auth_time?: number;
  session_state?: string;
  acr?: string;
  realm_access?: {
    roles?: string[];
  };
  resource_access?: KeycloakResourceAccess;
  scope?: string;
  name?: string;
  preferred_username?: string;
  given_name?: string;
  family_name?: string;
  email?: string;
};
