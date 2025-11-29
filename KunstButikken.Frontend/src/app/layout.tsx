import 'reflect-metadata';
import Providers from "@/shared/providers/Providers";
import "@/styles/globals.css";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v15-appRouter";
import type { Metadata, Viewport } from "next";
import React from "react";

// Use Keycloak-based navbar (client component with useKeycloak hook)
// Similar to Angular's navbar that uses AuthService/KeycloakService
import NavbarKeycloak from "@/features/navigation/components/NavbarKeycloak";

// Server-side metadata generation
// Similar to Angular's index.html metadata but dynamic
export const metadata: Metadata = {
  title: "KunstButikken",
  description: "Art marketplace - Discover and purchase unique artworks",
  icons: {
    icon: "/favicon.ico",
  },
};

// Viewport configuration (separate from metadata in Next.js 14+)
export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
};

/**
 * Root layout component for the Next.js app with SSR support
 *
 * This layout is rendered on the server by default, providing:
 * - SEO-friendly metadata
 * - Initial HTML structure before client hydration
 * - Consistent layout across all pages
 *
 * Similar to Angular's app.component.ts but with server-side rendering capabilities
 *
 * The script tag prevents theme/language flash by reading localStorage before hydration,
 * ensuring a smooth user experience similar to Angular's SSR approach.
 */
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="nb" data-theme="light" suppressHydrationWarning>
      <body>
        {/* Prevent theme and language flash: read from localStorage before hydration */}
        <script
          dangerouslySetInnerHTML={{
            __html:
              "(function(){try{var d=document.documentElement;var ls=localStorage;var m=ls.getItem('mui-mode');if(m!=='light'&&m!=='dark'){m=window.matchMedia&&window.matchMedia('(prefers-color-scheme: dark)').matches?'dark':'light'};d.setAttribute('data-theme',m);var lang=ls.getItem('kb_locale');if(!lang){var n=(navigator.language||'').split('-')[0];lang=(n==='en'||'nb')?n:'nb'};d.setAttribute('lang',lang);}catch(e){}})();",
          }}
        />
        <AppRouterCacheProvider options={{ enableCssLayer: true }}>
          <Providers>
            <NavbarKeycloak />
            {/* Mobile-first responsive container: progressively wider at sm/md/lg/xl */}
            <main>
              <div style={{ margin: '0 auto', width: '100%', paddingLeft: '16px', paddingRight: '16px', paddingTop: '16px', paddingBottom: '16px', maxWidth: '1400px' }}>
                {children}
              </div>
            </main>
          </Providers>
        </AppRouterCacheProvider>
      </body>
    </html>
  );
}
