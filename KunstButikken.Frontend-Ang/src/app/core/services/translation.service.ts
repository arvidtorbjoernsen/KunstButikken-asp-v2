import { isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export interface LocaleOption {
  code: string;
  label: string;
}

@Injectable({ providedIn: 'root' })
export class TranslationService {
  private static readonly storageKey = 'kb-locale';
  private static readonly fallbackLocale = 'nb';
  private readonly isBrowser: boolean;
  private readonly localeInternal = signal<string>(TranslationService.fallbackLocale);
  readonly locale = this.localeInternal.asReadonly();

  readonly locales: LocaleOption[] = [
    { code: 'nb', label: 'Norsk' },
    { code: 'en', label: 'English' },
    { code: 'es', label: 'Español' },
    { code: 'pt-pt', label: 'Português (Portugal)' },
    { code: 'pt-br', label: 'Português (Brasil)' }
  ];

  constructor(
    private readonly translate: TranslateService,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
    this.isBrowser = isPlatformBrowser(this.platformId);
    this.translate.addLangs(this.locales.map((l) => l.code));
    this.translate.setDefaultLang(TranslationService.fallbackLocale);
    const initial = this.resolveInitialLocale();
    this.applyLocale(initial);
  }

  setLocale(locale: string): void {
    const lang = this.normalizeLocale(locale) ?? TranslationService.fallbackLocale;
    this.applyLocale(lang);
  }

  private resolveInitialLocale(): string {
    if (!this.isBrowser) {
      return TranslationService.fallbackLocale;
    }
    try {
      const stored = this.normalizeLocale(window.localStorage.getItem(TranslationService.storageKey));
      if (stored) {
        return stored;
      }

      const navigatorCandidates: string[] = [];
      const primaryNavigator = window.navigator.language;
      if (primaryNavigator) {
        navigatorCandidates.push(primaryNavigator);
      }
      if (Array.isArray(window.navigator.languages)) {
        navigatorCandidates.push(...window.navigator.languages);
      }

      for (const candidate of navigatorCandidates) {
        const normalized = this.normalizeLocale(candidate);
        if (normalized) {
          return normalized;
        }
      }
    } catch {
      // ignore errors and fall back
    }
    return TranslationService.fallbackLocale;
  }

  private normalizeLocale(value: string | null | undefined): string | null {
    if (!value) {
      return null;
    }

    const lower = value.toLowerCase();
    const directMatch = this.locales.find((locale) => locale.code === lower);
    if (directMatch) {
      return directMatch.code;
    }

    const base = lower.split('-')[0];

    const exactBase = this.locales.find((locale) => locale.code === base);
    if (exactBase) {
      return exactBase.code;
    }

    const extendedMatch = this.locales.find((locale) => locale.code.startsWith(`${base}-`));
    return extendedMatch?.code ?? null;
  }

  private persistLocale(locale: string): void {
    if (!this.isBrowser) {
      return;
    }
    try {
      window.localStorage.setItem(TranslationService.storageKey, locale);
    } catch {
      // noop
    }
  }

  private applyLocaleToDocument(locale: string): void {
    if (!this.isBrowser) {
      return;
    }
    try {
      const [language, region] = locale.split('-');
      document.documentElement.lang = region
        ? `${language}-${region.toUpperCase()}`
        : language;
    } catch {
      // noop
    }
  }

  private applyLocale(locale: string): void {
    this.localeInternal.set(locale);
    this.translate.use(locale);
    this.persistLocale(locale);
    this.applyLocaleToDocument(locale);
  }
}
