import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth';

/**
 * Restricts booking management to internal users only.
 */
export const bookingInternalGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  if (!isPlatformBrowser(platformId)) {
    return true;
  }

  auth.restoreSession();
  if (auth.isGuest()) {
    return router.createUrlTree(['/login']);
  }

  const role = auth.getRole();
  const isInternalBookingUser = role === 'Doctor' || role === 'Administrator' || role === 'RootAdmin';
  return isInternalBookingUser ? true : router.createUrlTree(['/patients']);
};
