import { APP_INITIALIZER, ApplicationConfig } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { apiBaseUrlInterceptor } from './interceptors/api-base-url.interceptor';
import { apiRequestLogInterceptor } from './interceptors/api-request-log.interceptor';
import { authInterceptor } from './interceptors/auth.interceptor';
import { errorInterceptor } from './interceptors/error.interceptor';
import { AuthService } from './services/auth';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(
      withFetch(),
      withInterceptors([
        apiBaseUrlInterceptor,
        apiRequestLogInterceptor,
        errorInterceptor,
        authInterceptor,
      ]),
    ),
    {
      provide: APP_INITIALIZER,
      deps: [AuthService],
      multi: true,
      useFactory: (auth: AuthService) => () => auth.initAuth()
    }
  ]
};
