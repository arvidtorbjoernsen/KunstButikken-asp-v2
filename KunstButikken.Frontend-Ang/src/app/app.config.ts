import { isPlatformBrowser } from '@angular/common';
import { HttpClient, provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import {
  APP_INITIALIZER,
  ApplicationConfig,
  importProvidersFrom,
  Inject,
  Injectable,
  PLATFORM_ID,
  provideZonelessChangeDetection
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslationObject } from '@ngx-translate/core';
import { catchError, from, of } from 'rxjs';
import { routes } from './app.routes';
import { authTokenInterceptor } from './core/interceptors/auth-token.interceptor';
import { logTokenInterceptor } from './core/interceptors/log-token.interceptor';
import { KeycloakService } from './core/services/keycloak.service';

@Injectable()
class AssetTranslateLoader implements TranslateLoader {
  constructor(
    private readonly http: HttpClient,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
  }

  getTranslation(lang: string) {
    const safeLang = (lang ?? '').trim().toLowerCase();
    if (!safeLang) {
      return of({} as TranslationObject);
    }

    const assetPath = `/assets/i18n/${safeLang}.json`;

    if (isPlatformBrowser(this.platformId)) {
      return this.http
        .get<TranslationObject>(assetPath)
        .pipe(catchError(() => of({} as TranslationObject)));
    }

    // Server-side: read from filesystem
    return from(
      (async () => {
        try {
          const fs = await import('fs/promises' as any);
          const path = await import('path' as any);

          const searchRoots = [
            path.join(
              process.cwd(),
              'dist',
              'KunstButikken.Frontend-Ang',
              'browser',
              'assets',
              'i18n'
            ),
            path.join(process.cwd(), 'src', 'assets', 'i18n')
          ];

          for (const root of searchRoots) {
            try {
              const fileContent = await fs.readFile(path.join(root, `${safeLang}.json`), 'utf8');
              return JSON.parse(fileContent) as TranslationObject;
            } catch {
              // try next location
            }
          }
        } catch {
          // ignore and fall through
        }

        return {} as TranslationObject;
      })()
    ).pipe(catchError(() => of({} as TranslationObject)));
  }
}

// Factory function to initialize Keycloak before app starts
function initializeKeycloak(keycloakService: KeycloakService) {
  return () => keycloakService.boot();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes),
    // Initialize Keycloak before app starts
    {
      provide: APP_INITIALIZER,
      useFactory: initializeKeycloak,
      deps: [KeycloakService],
      multi: true
    },
    // Provide HTTP client with interceptors for both browser and server
    // The interceptors themselves will check if they're in browser context
    provideHttpClient(withFetch(), withInterceptors([authTokenInterceptor, logTokenInterceptor])),
    importProvidersFrom(
      TranslateModule.forRoot({
        fallbackLang: 'nb',
        loader: {
          provide: TranslateLoader,
          useClass: AssetTranslateLoader,
          deps: [HttpClient, PLATFORM_ID]
        }
      })
    ),
  ]
};
