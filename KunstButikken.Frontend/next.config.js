/* eslint-disable @typescript-eslint/no-require-imports */
/**
 * next.config.js
 * Next.js 16 configuration with SSR enabled.
 * Similar to Angular SSR setup, this ensures server-side rendering for better performance and SEO.
 */

/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,

  // Explicitly disable Turbopack for stability (similar to dev script)
  // Remove this once Turbopack is stable in Next.js 16+

  // Enable proper SSR (this is the default in Next.js, but we're being explicit)

  // Note: We don't use the 'env' property here because it evaluates at build/startup time.
  // Server-side code (API routes) can directly access process.env variables passed by AppHost.
  // The experimental.serverActions property allows runtime environment variables.
  experimental: {
    serverActions: {
      allowedOrigins: ['localhost:3000'],
    },
  },

  // Image optimization configuration
  images: {
    remotePatterns: [
      {
        protocol: 'http',
        hostname: 'localhost',
      },
      {
        protocol: 'http',
        hostname: '127.0.0.1',
      },
    ],
  },
};

const requiredPolyfillFiles = [
  './src/app/layout.tsx',
  './src/shared/providers/Providers.tsx',
];
const reflectImportPattern = /import\s+['"]reflect-metadata['"]/;
for (const file of requiredPolyfillFiles) {
  const contents = require('fs').readFileSync(require('path').resolve(__dirname, file), 'utf8');
  if (!reflectImportPattern.test(contents)) {
    throw new Error(`Missing "import 'reflect-metadata'" in ${file}. DI requires this polyfill.`);
  }
}

module.exports = nextConfig;


// Injected content via Sentry wizard below

const { withSentryConfig } = require('@sentry/nextjs');

module.exports = withSentryConfig(
  module.exports,
  {
    // For all available options, see:
    // https://www.npmjs.com/package/@sentry/webpack-plugin#options

    org: 'arvid-torbjrnsen',
    project: 'javascript-nextjs',

    // Only print logs for uploading source maps in CI
    silent: !process.env.CI,

    // For all available options, see:
    // https://docs.sentry.io/platforms/javascript/guides/nextjs/manual-setup/

    // Upload a larger set of source maps for prettier stack traces (increases build time)
    widenClientFileUpload: true,

    // Route browser requests to Sentry through a Next.js rewrite to circumvent ad-blockers.
    // This can increase your server load as well as your hosting bill.
    // Note: Check that the configured route will not match with your Next.js middleware, otherwise reporting of client-
    // side errors will fail.
    tunnelRoute: '/monitoring',

    // Automatically tree-shake Sentry logger statements to reduce bundle size
    disableLogger: true,

    // Enables automatic instrumentation of Vercel Cron Monitors. (Does not yet work with App Router route handlers.)
    // See the following for more information:
    // https://docs.sentry.io/product/crons/
    // https://vercel.com/docs/cron-jobs
    automaticVercelMonitors: true,
  },
  {
    // Upload a larger set of source maps for prettier stack traces (increases build time)
    widenClientFileUpload: true,
    // ...
    instrumenter: 'sentry',
  },
);
