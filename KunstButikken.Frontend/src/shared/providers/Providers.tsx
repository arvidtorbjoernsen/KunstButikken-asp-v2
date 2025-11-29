"use client";

import "reflect-metadata";

import { TranslationProvider } from "@/features/i18n/components";
import { KeycloakProvider } from "@/features/auth/lib/keycloak";
import React from "react";
import ReduxProvider from "./ReduxProvider";
import ThemeProvider from "./ThemeProvider";
import DiProvider from "@/presentation/providers/DiProvider";

/**
 * Main providers wrapper for the application
 * Composes all client-side providers needed for the app
 * Similar to Angular's app.config.ts but using React context providers
 *
 * Order matters:
 * 1. KeycloakProvider - Direct Keycloak authentication (like Angular's KeycloakService)
 * 2. ThemeProvider - Material-UI theme and dark/light mode
 * 3. TranslationProvider - i18n translations
 * 4. ReduxProvider - Global state management
 */
export default function Providers({ children }: { children: React.ReactNode }) {
  return (
    <DiProvider>
      <KeycloakProvider>
        <ThemeProvider>
          <TranslationProvider>
            <ReduxProvider>{children}</ReduxProvider>
          </TranslationProvider>
        </ThemeProvider>
      </KeycloakProvider>
    </DiProvider>
  );
}
