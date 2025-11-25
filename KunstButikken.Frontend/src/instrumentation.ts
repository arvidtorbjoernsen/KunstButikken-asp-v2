// This file configures the initialization of Sentry on the server.
// The added config here will be used whenever the server handles a request.
// https://docs.sentry.io/platforms/javascript/guides/nextjs/

import * as Sentry from '@sentry/nextjs';

Sentry.init({
  dsn: 'https://8a54a560e0e84a09bdc62642d144a8c5@o4504588644188160.ingest.us.sentry.io/4504588713066498',

  // Adjust this value in production, or use tracesSampler for greater control
  tracesSampleRate: 1.0,

  // Setting this option to true will print useful information to the console while you're setting up Sentry.
  debug: false,
});