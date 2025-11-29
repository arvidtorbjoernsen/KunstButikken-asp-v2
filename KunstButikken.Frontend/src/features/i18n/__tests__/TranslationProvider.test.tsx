import React from 'react';
import { renderHook, act } from '@testing-library/react';
import { TranslationProvider, useTranslations } from '@/features/i18n/components/TranslationProvider';

function wrapper({ children, defaultLocale = 'nb' }: any) {
  return <TranslationProvider defaultLocale={defaultLocale}>{children}</TranslationProvider>;
}

describe('TranslationProvider', () => {
  beforeEach(() => {
    // Clear storage and mocks
    try { localStorage.clear(); } catch {}
    // Reset navigator language
    // @ts-ignore
    delete global.navigator;
    // reset document
    // @ts-ignore
    if (global.document && document.documentElement) {
      document.documentElement.lang = '';
    }
  });

  test('initializes from localStorage if present', () => {
    try { localStorage.setItem('kb_locale', 'en'); } catch {}

    const { result } = renderHook(() => useTranslations(), { wrapper });
    expect(result.current.locale).toBe('en');
  });

  test('falls back to navigator language when storage empty', () => {
    // @ts-ignore
    global.navigator = { language: 'en-US' };
    const { result } = renderHook(() => useTranslations(), { wrapper });
    expect(result.current.locale).toBe('en');
  });

  test('setLocale stores value and changes locale only for supported', () => {
    const { result } = renderHook(() => useTranslations(), { wrapper });
    act(() => result.current.setLocale('en'));
    expect(result.current.locale).toBe('en');
    expect(localStorage.getItem('kb_locale')).toBe('en');

    // unsupported locale should be ignored
    act(() => result.current.setLocale('zz'));
    expect(result.current.locale).toBe('en');
  });

  test('t looks up nested keys and interpolates', () => {
    const { result } = renderHook(() => useTranslations(), { wrapper });
    act(() => result.current.setLocale('en'));
    const t = result.current.t;
    expect(t('nav.home')).toBe('Home');

    // interpolation with simple var
    expect(t('form.newArt.title')).toBe('Register New Art');

    // unknown key returns the key
    expect(t('not.exists')).toBe('not.exists');

    // interpolation template
    // use nb template which contains placeholder? create dynamic template via interpolate by calling t with a string containing {{var}}
    const str = 'Hello {{name}}';
    // Direct call to internal interpolate isn't exported; but t falls back to interpolate when missing key
    expect(t(str, { name: 'Arvid' })).toBe('Hello Arvid');
  });

  // New tests to increase branch coverage
  test('handles localStorage.getItem throwing and defaults to defaultLocale', () => {
    // make getItem throw
    const originalGet = localStorage.getItem;
    // @ts-ignore
    localStorage.getItem = () => { throw new Error('no'); };
    // remove navigator to force default
    // @ts-ignore
    delete global.navigator;
    const { result } = renderHook(() => useTranslations(), { wrapper });
    expect(result.current.locale).toBe('nb');
    // restore
    // @ts-ignore
    localStorage.getItem = originalGet;
  });

  test('document.lang setAttribute exception is swallowed', () => {
    // @ts-ignore
    global.navigator = { language: 'en-US' };
    // make setAttribute throw
    const orig = document.documentElement.setAttribute;
    // @ts-ignore
    document.documentElement.setAttribute = () => { throw new Error('boom'); };
    const { result } = renderHook(() => useTranslations(), { wrapper });
    act(() => result.current.setLocale('en'));
    // still sets locale in state
    expect(result.current.locale).toBe('en');
    // restore
    // @ts-ignore
    document.documentElement.setAttribute = orig;
  });

  test('interpolate supports nested vars and missing nested returns empty string', () => {
    const { result } = renderHook(() => useTranslations(), { wrapper });
    const t = result.current.t;
    const template = 'User {{user.name.first}} {{user.name.last}}';
    const filled = t(template, { user: { name: { first: 'A' } } as any });
    expect(filled).toBe('User A ');
  });
});
