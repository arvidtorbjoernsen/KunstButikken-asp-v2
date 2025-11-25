

import {environment} from '../environments/environment';
import {provideKeycloak, withAutoRefreshToken} from 'keycloak-angular';

provideKeycloak({
  config: {
    url: environment.keycloak.baseUrl,
    realm: 'kunstbutikken',
    clientId: environment.keycloak.clientId,
  },
  initOptions: {
    onLoad: 'check-sso',
    silentCheckSsoRedirectUri: typeof window !== 'undefined'
      ? `${window.location.origin}/assets/silent-check-sso.html`
      : undefined,
    redirectUri: typeof window !== 'undefined' ? window.location.origin : undefined,
    checkLoginIframe: false,
  },
  features: [withAutoRefreshToken()], // ✅ Correct usage
});
