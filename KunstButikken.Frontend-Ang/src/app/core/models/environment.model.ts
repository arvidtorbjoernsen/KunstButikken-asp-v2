export interface RuntimeEnvironment {
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
    gateway: string;
  };
  keycloak: {
    baseUrl: string;
    realm: string;
    issuer: string;
    clientId: string;
    clientSecret?: string;
  };
}
