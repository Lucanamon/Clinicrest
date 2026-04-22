import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth';

/** `/` → `/login` when unauthenticated, `/patients` when signed in. */
export const rootRedirectGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);
  const requestedUrl = state.url || '/';

  // Safety: this guard should only redirect the true root URL.
  // During hydration/initial navigation, it may run before the final URL settles.
  if (requestedUrl !== '/' && requestedUrl !== '') {
    return true;
  }

  if (!isPlatformBrowser(platformId)) {
    return router.createUrlTree(['/login']);
  }

  auth.restoreSession();
  return auth.isLoggedIn() ? router.createUrlTree(['/patients']) : router.createUrlTree(['/login']);
};
