import { CommonModule } from '@angular/common';
import { Component, effect, inject, PLATFORM_ID, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { KeycloakService } from '../../../core/services/keycloak.service';
import { SearchService } from '../../../core/services/search.service';
import { ThemeService } from '../../../core/services/theme.service';
import { TranslationService } from '../../../core/services/translation.service';

type NavLink = Readonly<{ key: string; path: string; icon: string }>;

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    MatChipsModule,
    TranslateModule,
  ],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
})
export class NavbarComponent {
  readonly isAuthenticated = signal(false);
  readonly username = signal('');
  readonly isSearchOpen = signal(false);
  readonly primaryLinks: NavLink[] = [
    { key: 'nav.home', path: '/', icon: 'home' },
    { key: 'nav.artForSale', path: '/art', icon: 'storefront' },
    { key: 'nav.auctions', path: '/auctions', icon: 'gavel' },
  ];
  private readonly themeService = inject(ThemeService);
  private readonly translationService = inject(TranslationService);
  private readonly keycloakService = inject(KeycloakService);
  private readonly searchService = inject(SearchService);
  readonly locales = this.translationService.locales;
  private readonly platformId = inject(PLATFORM_ID);

  constructor() {
    // React to authentication state changes from KeycloakService
    effect(() => {
      const authenticated = this.keycloakService.authenticated();
      console.log('[NavbarComponent] Auth state changed:', authenticated);
      this.isAuthenticated.set(authenticated);
    });
  }

  get themeIcon(): string {
    return this.themeService.mode() === 'dark' ? 'dark_mode' : 'light_mode';
  }

  get activeLocaleLabel(): string {
    const locale = this.translationService.locale();
    return this.translationService.locales.find((l) => l.code === locale)?.label ?? '';
  }

  get activeLocale(): string {
    return this.translationService.locale();
  }

  get activeLocaleFlag(): string {
    const code = this.translationService.locale().toLowerCase();
    switch (code) {
      case 'nb':
      case 'no':
        return '🇳🇴';
      case 'es':
        return '🇪🇸';
      case 'pt-pt':
      case 'pt':
        return '🇵🇹';
      case 'pt-br':
        return '🇧🇷';
      case 'en':
      default:
        return '🇬🇧';
    }
  }

  toggleTheme(): void {
    this.themeService.toggle();
  }

  setLocale(locale: string): void {
    this.translationService.setLocale(locale);
  }

  login(): void {
    this.keycloakService.login();
  }

  logout(): void {
    this.keycloakService.logout();
  }

  debugToken(): void {
    console.log('Current token:', this.keycloakService.getToken());
  }

  toggleSearch(): void {
    this.isSearchOpen.set(!this.isSearchOpen());
  }

  onSearch(event: Event): void {
    const query = (event.target as HTMLInputElement).value;
    this.searchService.setQuery(query);
  }
}
