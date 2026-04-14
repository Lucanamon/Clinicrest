import { HttpInterceptorFn } from '@angular/common/http';
import { isDevMode } from '@angular/core';

/**
 * In dev builds, logs each outgoing HTTP request URL (relative + query).
 * Helps verify calls hit `/api/*` (proxied) instead of the SPA (HTML).
 */
export const apiRequestLogInterceptor: HttpInterceptorFn = (req, next) => {
  if (isDevMode()) {
    console.debug(`[HTTP] ${req.method} ${req.urlWithParams}`);
  }
  return next(req);
};
