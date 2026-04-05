import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject, Observable, tap } from 'rxjs';

const TOKEN_KEY = 'clinicrest.token';
const USERNAME_KEY = 'clinicrest.username';
const ROLE_KEY = 'clinicrest.role';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  role: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly authenticated = new BehaviorSubject<boolean>(false);

  /** Emits when login state changes; on the client, matches persisted token presence. */
  readonly authState$ = this.authenticated.asObservable();

  constructor(
    private readonly http: HttpClient,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
    if (isPlatformBrowser(this.platformId)) {
      this.authenticated.next(!!this.getStorageItem(TOKEN_KEY));
    }
  }

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', payload).pipe(
      tap((response) => {
        this.setStorageItem(TOKEN_KEY, response.token);
        this.setStorageItem(USERNAME_KEY, response.username);
        this.setStorageItem(ROLE_KEY, response.role);
        this.authenticated.next(true);
      })
    );
  }

  logout(): void {
    this.removeStorageItem(TOKEN_KEY);
    this.removeStorageItem(USERNAME_KEY);
    this.removeStorageItem(ROLE_KEY);
    this.authenticated.next(false);
  }

  getToken(): string | null {
    return this.getStorageItem(TOKEN_KEY);
  }

  getUsername(): string {
    return this.getStorageItem(USERNAME_KEY) ?? 'User';
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
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
}
