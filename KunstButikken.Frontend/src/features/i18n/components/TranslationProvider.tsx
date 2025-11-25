"use client";

import en from "@/features/i18n/locales/en.json";
import nb from "@/features/i18n/locales/nb.json";
import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";

type InterpolationValues = Record<string, string | number | boolean | null | undefined>;

type TFunc = (key: string, vars?: InterpolationValues) => string;

// Recursive resource tree: keys map to either strings or deeper trees
type ResourceTree = { [key: string]: string | ResourceTree };

const resources: Record<string, ResourceTree> = { nb, en } as const;

const STORAGE_KEY = "kb_locale";

const TranslationContext = createContext<{
  locale: string;
  setLocale: (l: string) => void;
  t: TFunc;
}>({
  locale: "nb",
  setLocale: () => {},
  t: (k: string) => k,
});

export function TranslationProvider({ children, defaultLocale = "nb" }: { children: React.ReactNode; defaultLocale?: string }) {
  const [locale, setLocaleState] = useState<string>(defaultLocale);

  // Initialize from localStorage or navigator
  useEffect(() => {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved && saved in resources) {
        setLocaleState(saved);
        return;
      }
    } catch {
      // ignore
    }

    if (typeof navigator !== "undefined") {
      type NavigatorWithLang = Navigator & { userLanguage?: string };
      const navLang = navigator.language || (navigator as NavigatorWithLang).userLanguage || "";
      const primary = navLang.split("-")[0];
      if (primary && primary in resources) {
        setLocaleState(primary);
        return;
      }
    }

    setLocaleState(defaultLocale);
  }, [defaultLocale]);

  const setLocale = useCallback((l: string) => {
    if (!(l in resources)) return;
    try {
      localStorage.setItem(STORAGE_KEY, l);
    } catch {
      // ignore
    }
    setLocaleState(l);
  }, []);

  // Keep <html lang> in sync for a11y and to avoid language flash
  useEffect(() => {
    try {
      document.documentElement.setAttribute('lang', locale);
    } catch {}
  }, [locale]);

  const t: TFunc = useCallback((key: string, vars?: InterpolationValues) => {
    const parts = key.split(".");
    let obj: unknown = resources[locale] || resources[defaultLocale] || resources.nb;
    for (const p of parts) {
      if (obj && typeof obj === "object" && p in (obj as Record<string, unknown>)) {
        obj = (obj as Record<string, unknown>)[p];
      } else {
        return interpolate(key, vars);
      }
    }
    if (typeof obj === "string") return interpolate(obj as string, vars);
    return interpolate(key, vars);
  }, [locale, defaultLocale]);

  const value = useMemo(() => ({ locale, setLocale, t }), [locale, setLocale, t]);

  return <TranslationContext.Provider value={value}>{children}</TranslationContext.Provider>;
}

function interpolate(template: string, vars?: InterpolationValues) {
  if (!vars) return template;
  return template.replace(/{{\s*([\w.]+)\s*}}/g, (_, name) => {
    const parts = name.split('.');
    let v: unknown = vars;
    for (const p of parts) {
      if (v == null || typeof v !== 'object') return '';
      v = (v as Record<string, unknown>)[p];
    }
    return v == null ? '' : String(v);
  });
}

export const useTranslations = () => useContext(TranslationContext);
