import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express, { type NextFunction, type Request, type Response } from 'express';
import http from 'node:http';
import https from 'node:https';
import { join } from 'node:path';

const browserDistFolder = join(import.meta.dirname, '../browser');

const app = express();
const apiRouter = express.Router();
const angularApp = new AngularNodeAppEngine();

/**
 * Upstream .NET API (Kestrel). Default matches `npm run dev:api` (--urls http://localhost:5001).
 * Override in production: API_UPSTREAM=https://api.example.com
 */
const apiUpstreamBase = (process.env['API_UPSTREAM'] ?? 'http://127.0.0.1:5001').replace(/\/$/, '');

/**
 * Log every request and the response Content-Type when the response finishes.
 */
app.use((req, res, next) => {
  res.on('finish', () => {
    const ct = res.getHeader('content-type');
    const ctStr = typeof ct === 'string' ? ct : Array.isArray(ct) ? ct.join(',') : '';
    console.log(
      `[${new Date().toISOString()}] ${req.method} ${req.originalUrl} -> ${res.statusCode} content-type: ${ctStr || '(none)'}`,
    );
  });
  next();
});

/**
 * Never serve the Angular HTML shell for `/api/*`.
 * Proxy to the real API so clients always receive JSON (or API error payloads), not index.html.
 */
apiRouter.use((req, res, next) => {
  console.log(`[API] ${req.method} ${req.url}`);
  next();
});

apiRouter.use((clientReq, clientRes, next) => {
  const targetUrl = new URL(clientReq.originalUrl, `${apiUpstreamBase}/`);

  const lib = targetUrl.protocol === 'https:' ? https : http;
  const opts: http.RequestOptions = {
    protocol: targetUrl.protocol,
    hostname: targetUrl.hostname,
    port: targetUrl.port || (targetUrl.protocol === 'https:' ? 443 : 80),
    path: `${targetUrl.pathname}${targetUrl.search}`,
    method: clientReq.method,
    headers: { ...clientReq.headers, host: targetUrl.host },
  };

  const upstreamLabel = `${opts.method} ${apiUpstreamBase}${opts.path}`;
  const proxyReq = lib.request(opts, (proxyRes) => {
    const ct = proxyRes.headers['content-type'];
    const ctStr = Array.isArray(ct) ? ct.join(',') : (ct ?? '');
    console.log(
      `[API PROXY] ${upstreamLabel} -> ${proxyRes.statusCode ?? 0} content-type: ${ctStr || '(none)'}`,
    );
    clientRes.writeHead(proxyRes.statusCode ?? 502, proxyRes.headers);
    proxyRes.pipe(clientRes);
  });

  proxyReq.on('error', (err) => {
    if (!clientRes.headersSent) {
      next(err);
    }
  });

  clientReq.pipe(proxyReq);
});

app.use('/api', apiRouter);

app.use('/api', (req, res) => {
  if (res.headersSent) {
    return;
  }
  res.status(404).json({
    error: 'API route not found',
    path: req.originalUrl,
  });
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
 * JSON 404 when nothing else handled the request (never HTML for unknown paths here).
 */
app.use((req, res) => {
  if (res.headersSent) {
    return;
  }
  res.status(404).type('application/json').json({ message: 'Not Found' });
});

app.use((err: unknown, _req: Request, res: Response, _next: NextFunction) => {
  if (res.headersSent) {
    return;
  }
  const message = err instanceof Error ? err.message : String(err);
  res.status(500).json({
    error: 'Internal Server Error',
    message,
  });
});

/**
 * Start the server if this module is the main entry point, or it is ran via PM2.
 */
if (isMainModule(import.meta.url) || process.env['pm_id']) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, (error) => {
    if (error) {
      throw error;
    }

    console.log(`Node Express server listening on http://localhost:${port}`);
    console.log(`API proxy: /api/* -> ${apiUpstreamBase}/*`);
  });
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build) or Firebase Cloud Functions.
 */
export const reqHandler = createNodeRequestHandler(app);
