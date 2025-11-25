import { isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID, effect, signal } from '@angular/core';

type ThemeMode = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'kb-theme';
  private readonly isBrowser: boolean;
  private readonly modeInternal = signal<ThemeMode>('light');
  readonly mode = this.modeInternal.asReadonly();

  constructor(@Inject(PLATFORM_ID) private readonly platformId: object) {
    this.isBrowser = isPlatformBrowser(this.platformId);
    const initial = this.readStoredTheme();
    this.modeInternal.set(initial);
    this.applyTheme(initial);

    effect(() => {
      const mode = this.modeInternal();
      this.persistTheme(mode);
      this.applyTheme(mode);
    });
  }

  toggle(): void {
    this.modeInternal.update((current) => (current === 'dark' ? 'light' : 'dark'));
  }

  private readStoredTheme(): ThemeMode {
    if (!this.isBrowser) {
      return 'light';
    }
    try {
      const stored = window.localStorage.getItem(this.storageKey);
      if (stored === 'light' || stored === 'dark') {
        return stored;
      }
      return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches
        ? 'dark'
        : 'light';
    } catch {
      return 'light';
    }
  }

  private persistTheme(mode: ThemeMode): void {
    if (!this.isBrowser) {
      return;
    }
    try {
      window.localStorage.setItem(this.storageKey, mode);
    } catch {
      // noop
    }
  }

  private applyTheme(mode: ThemeMode): void {
    if (!this.isBrowser) {
      return;
    }
    try {
      document.documentElement.dataset['theme'] = mode;
    } catch {
      // noop
    }
  }
}
