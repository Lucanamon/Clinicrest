import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../environments/environment';

/**
 * When `environment.apiBaseUrl` is set, prepends it to relative `/api` requests.
 */
export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  const base = environment.apiBaseUrl?.trim();
  const apiPrefix = (environment.apiUrl ?? '/api').replace(/\/$/, '');
  if (!base || !req.url.startsWith(apiPrefix)) {
    return next(req);
  }

  const normalized = base.replace(/\/$/, '');
  return next(req.clone({ url: `${normalized}${req.url}` }));
};
