import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { KeycloakService } from '../services/keycloak.service';

export const authTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const keycloakService = inject(KeycloakService);

  // Check if this request should have the token attached
  // Only attach to requests going to our API gateway
  const isApiRequest = req.url.startsWith(environment.apis.gateway);

  if (!isApiRequest) {
    console.debug('[authTokenInterceptor] Skipping token for non-API request:', req.url);
    return next(req);
  }

  const token = keycloakService.getToken();

  if (!token) {
    console.warn('[authTokenInterceptor] No token available for API request:', req.url);
    return next(req);
  }

  // Clone the request and add the Authorization header
  const authReq = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  console.debug('[authTokenInterceptor] Token attached to request:', req.url);

  return next(authReq);
};
