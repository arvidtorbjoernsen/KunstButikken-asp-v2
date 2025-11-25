// src/app/core/interceptors/log-token.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { KeycloakService } from '../services/keycloak.service';

export const logTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const keycloakService = inject(KeycloakService);
  const token = keycloakService.getToken();

  console.debug('[logTokenInterceptor] Outgoing request:', req.url);
  console.debug('[logTokenInterceptor] Token exists:', !!token);
  if (token) {
    console.debug('[logTokenInterceptor] Token (first 50 chars):', token.substring(0, 50) + '...');
  }

  return next(req);
};
