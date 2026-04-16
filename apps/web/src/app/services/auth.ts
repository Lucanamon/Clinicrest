import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject, Observable, of, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

const TOKEN_KEY = 'clinicrest.token';
const USERNAME_KEY = 'clinicrest.username';
const ROLE_KEY = 'clinicrest.role';
const USER_ID_KEY = 'clinicrest.userId';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  role: string;
  userId: string;
}

export interface CurrentUserProfile {
  id: string;
  username: string;
  displayName?: string | null;
  role: string;
  profileImageUrl?: string | null;
  lastActiveAt: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly authenticated = new BehaviorSubject<boolean>(false);
  private readonly currentUserProfileSubject = new BehaviorSubject<CurrentUserProfile | null>(null);
  private currentUser: Record<string, unknown> | null = null;

  /** Emits when login state changes; on the client, matches persisted token presence. */
  readonly authState$ = this.authenticated.asObservable();
  readonly currentUserProfile$ = this.currentUserProfileSubject.asObservable();

  constructor(
    private readonly http: HttpClient,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
    if (isPlatformBrowser(this.platformId)) {
      this.authenticated.next(!!this.getStorageItem(TOKEN_KEY));
    }
  }

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, payload).pipe(
      tap((response) => {
        this.setStorageItem(TOKEN_KEY, response.token);
        this.setStorageItem(USERNAME_KEY, response.username);
        this.setStorageItem(ROLE_KEY, response.role);
        this.setStorageItem(USER_ID_KEY, response.userId);
        this.authenticated.next(true);
      })
    );
  }

  logout(): void {
    this.removeStorageItem(TOKEN_KEY);
    this.removeStorageItem(USERNAME_KEY);
    this.removeStorageItem(ROLE_KEY);
    this.removeStorageItem(USER_ID_KEY);
    this.currentUser = null;
    this.currentUserProfileSubject.next(null);
    this.authenticated.next(false);
  }

  getToken(): string | null {
    return this.getStorageItem(TOKEN_KEY);
  }

  initAuth(): void {
    this.restoreSession();
  }

  /**
   * Rehydrates auth state from persisted token after full page reloads.
   * Returns true when a valid token exists and user state is restored.
   */
  restoreSession(): boolean {
    const token = this.getToken();
    if (!token) {
      this.currentUser = null;
      this.authenticated.next(false);
      return false;
    }

    return this.setUserFromToken(token);
  }

  /**
   * Persists token-derived user state and marks the user as authenticated.
   * Invalid/expired tokens are cleared and treated as unauthenticated.
   */
  setUserFromToken(token: string): boolean {
    if (!this.isPlatformBrowser() || !this.isTokenValid(token)) {
      this.logout();
      return false;
    }

    const claims = this.decodeJwtPayload(token);
    this.currentUser = claims;
    const username = this.readClaim(claims, ['username', 'unique_name', 'name']);
    const role = this.readClaim(claims, [
      'role',
      'roles',
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
    ]);
    const userId = this.readClaim(claims, [
      'userId',
      'sub',
      'nameid',
      'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
    ]);

    this.setStorageItem(TOKEN_KEY, token);
    if (username) {
      this.setStorageItem(USERNAME_KEY, username);
    }
    if (role) {
      this.setStorageItem(ROLE_KEY, role);
    }
    if (userId) {
      this.setStorageItem(USER_ID_KEY, userId);
    }
    this.authenticated.next(true);
    return true;
  }

  getUsername(): string {
    return this.getStorageItem(USERNAME_KEY) ?? 'User';
  }

  getCurrentUserProfile(): CurrentUserProfile | null {
    return this.currentUserProfileSubject.value;
  }

  loadCurrentUserProfile(): Observable<CurrentUserProfile | null> {
    if (!this.getToken()) {
      this.currentUserProfileSubject.next(null);
      return of(null);
    }

    return this.http.get<CurrentUserProfile>(`${environment.apiUrl}/users/me`).pipe(
      tap((profile) => {
        this.currentUserProfileSubject.next(profile);
        if (profile.username) {
          this.setStorageItem(USERNAME_KEY, profile.username);
        }
      }),
      catchError(() => {
        this.currentUserProfileSubject.next(null);
        return of(null);
      })
    );
  }

  setCurrentUserProfile(profile: CurrentUserProfile): void {
    this.currentUserProfileSubject.next(profile);
    if (profile.username) {
      this.setStorageItem(USERNAME_KEY, profile.username);
    }
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    return !!token && this.isTokenValid(token);
  }

  isGuest(): boolean {
    return !this.getToken();
  }

  getUser(): Record<string, unknown> | null {
    return this.currentUser;
  }

  getRole(): string | null {
    return this.getStorageItem(ROLE_KEY);
  }

  getUserId(): string | null {
    return this.getStorageItem(USER_ID_KEY);
  }

  /** Full control of users and accounts (JWT claim). */
  isRootAdmin(): boolean {
    return this.getRole() === 'RootAdmin';
  }

  /** Any clinical role that may use the app (patients, appointments, etc.). */
  isClinicalStaff(): boolean {
    const r = this.getRole();
    return (
      r === 'RootAdmin' ||
      r === 'Doctor' ||
      r === 'Nurse' ||
      r === 'Administrator'
    );
  }

  canManagePatients(): boolean {
    return this.isClinicalStaff();
  }

  /** May choose which doctor an appointment is for (not the doctor role). */
  canSelectAppointmentDoctor(): boolean {
    const r = this.getRole();
    return r === 'RootAdmin' || r === 'Nurse' || r === 'Administrator';
  }

  isDoctor(): boolean {
    return this.getRole() === 'Doctor';
  }

  private getStorageItem(key: string): string | null {
    return isPlatformBrowser(this.platformId) ? localStorage.getItem(key) : null;
  }

  private setStorageItem(key: string, value: string): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(key, value);
    }
  }

  private removeStorageItem(key: string): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(key);
    }
  }

  private isPlatformBrowser(): boolean {
    return isPlatformBrowser(this.platformId);
  }

  private isTokenValid(token: string): boolean {
    const claims = this.decodeJwtPayload(token);
    if (!claims) {
      return false;
    }

    const expRaw = claims['exp'];
    if (typeof expRaw === 'number') {
      return expRaw * 1000 > Date.now();
    }

    if (typeof expRaw === 'string' && expRaw.trim() !== '') {
      const exp = Number(expRaw);
      return Number.isFinite(exp) && exp * 1000 > Date.now();
    }

    return true;
  }

  private decodeJwtPayload(token: string): Record<string, unknown> | null {
    try {
      const parts = token.split('.');
      if (parts.length < 2) {
        return null;
      }

      const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
      const decoded = atob(padded);
      return JSON.parse(decoded) as Record<string, unknown>;
    } catch {
      return null;
    }
  }

  private readClaim(claims: Record<string, unknown> | null, keys: string[]): string | null {
    if (!claims) {
      return null;
    }

    for (const key of keys) {
      const value = claims[key];
      if (typeof value === 'string' && value.trim() !== '') {
        return value;
      }
      if (Array.isArray(value) && typeof value[0] === 'string') {
        return value[0];
      }
    }
    return null;
  }
}
