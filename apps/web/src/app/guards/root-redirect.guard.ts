import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth';

/** `/` → `/login` when unauthenticated, `/patients` when signed in. */
export const rootRedirectGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  if (!isPlatformBrowser(platformId)) {
    return router.createUrlTree(['/login']);
  }

  auth.restoreSession();
  return auth.isLoggedIn() ? router.createUrlTree(['/patients']) : router.createUrlTree(['/login']);
};
