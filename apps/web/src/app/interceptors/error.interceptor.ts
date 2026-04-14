import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { catchError, throwError } from 'rxjs';

interface ApiErrorResponse {
  statusCode?: number;
  message?: string;
  detail?: string | null;
  traceId?: string;
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);

  return next(req).pipe(
    catchError((error: unknown) => {
      const message = extractErrorMessage(error);

      console.error('API request failed', {
        url: req.urlWithParams,
        method: req.method,
        message,
        error
      });

      if (isPlatformBrowser(platformId)) {
        window.alert(message);
      }

      return throwError(() => error);
    })
  );
};

function extractErrorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'An unexpected error occurred.';
  }

  const payload = error.error as ApiErrorResponse | string | null | undefined;

  if (typeof payload === 'string' && payload.trim() !== '') {
    return payload;
  }

  if (payload && typeof payload === 'object' && typeof payload.message === 'string' && payload.message.trim() !== '') {
    return payload.message;
  }

  if (typeof error.message === 'string' && error.message.trim() !== '') {
    return error.message;
  }

  return `Request failed with status ${error.status || 0}.`;
}
