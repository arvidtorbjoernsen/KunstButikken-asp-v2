import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import { join } from 'node:path';

// The Express app is exported so that it can be used by serverless Functions.
const app = express();
const angularApp = new AngularNodeAppEngine();

// --- Determine correct paths for dev vs. prod ---
const projectRoot = process.cwd();
const isDev = process.env['NODE_ENV'] === 'development';

// The browser distribution folder is where the client-side assets are located in production.
const browserDistFolder = join(projectRoot, 'dist/KunstButikken.Frontend-Ang/browser');
// The main index.html file.
const indexHtml = isDev
  ? join(projectRoot, 'src/index.html')
  : join(browserDistFolder, 'index.html');


/**
 * Set Content Security Policy to allow API connections with dynamic ports
 */
app.use((req, res, next) => {
  res.setHeader(
    'Content-Security-Policy',
    [
      "default-src 'self'",
      "script-src 'self' 'unsafe-inline' 'unsafe-eval'",
      "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
      "font-src 'self' https://fonts.gstatic.com",
      "img-src 'self' data: https: http://localhost:* http://127.0.0.1:*",
      "connect-src 'self' http://localhost:* http://127.0.0.1:* ws://localhost:* ws://127.0.0.1:*",
      "frame-src 'self' http://localhost:* http://127.0.0.1:*",
      "frame-ancestors 'none'",
      "base-uri 'self'",
      "form-action 'self'"
    ].join('; ')
  );
  next();
});

/**
 * Example Express Rest API endpoints can be defined here.
 */

/**
 * Exclude /dev-seed from server-side rendering by serving index.html,
 * allowing the client-side router to handle it.
 */
app.get('/dev-seed', (req, res) => {
  res.sendFile(indexHtml);
});

/**
 * Serve static files from /browser
 */
app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

/**
 * Handle all other requests by rendering the Angular application.
 */
app.use((req, res, next) => {
  angularApp
    .handle(req)
    .then((response) =>
      response ? writeResponseToNodeResponse(response, res) : next(),
    )
    .catch(next);
});

/**
 * Start the server if this module is the main entry point.
 */
if (isMainModule(import.meta.url)) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, (error) => {
    if (error) {
      throw error;
    }

    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build).
 */
export const reqHandler = createNodeRequestHandler(app);
