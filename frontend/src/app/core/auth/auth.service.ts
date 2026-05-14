import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, map, tap, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthResponse } from '../models/auth-response.model';
import { CurrentUser } from '../models/current-user.model';

const ACCESS_KEY = 'tc.accessToken';
const REFRESH_KEY = 'tc.refreshToken';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  private readonly _currentUser = signal<CurrentUser | null>(this.readInitialUser());

  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoggedIn = computed(() => this._currentUser() !== null);
  readonly isAdmin = computed(() => this._currentUser()?.roles.includes('Admin') ?? false);
  readonly isTrainer = computed(() => this._currentUser()?.roles.includes('Trainer') ?? false);

  login(email: string, password: string): Observable<void> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, { email, password }).pipe(
      tap(res => this.persistTokens(res)),
      map(() => void 0)
    );
  }

  /**
   * Public guest self-registration. Body intentionally has no password -
   * the backend creates the account inactive-until-set-password and
   * sends a welcome mail with a set-password token link. Response is
   * always 200 OK regardless of whether the address was new or taken
   * (enumeration protection mirrored from forgot-password).
   */
  register(email: string, firstName: string, lastName: string): Observable<void> {
    return this.http
      .post<{ message: string }>(`${this.baseUrl}/register`, { email, firstName, lastName })
      .pipe(map(() => void 0));
  }

  resetPassword(email: string, token: string, newPassword: string): Observable<void> {
    return this.http
      .post<void>(`${this.baseUrl}/reset-password`, { email, token, newPassword })
      .pipe(map(() => void 0));
  }

  refresh(): Observable<string> {
    const token = localStorage.getItem(REFRESH_KEY);
    if (!token) return throwError(() => new Error('no_refresh_token'));

    return this.http
      .post<AuthResponse>(`${this.baseUrl}/refresh`, { refreshToken: token })
      .pipe(
        tap(res => this.persistTokens(res)),
        map(res => res.accessToken)
      );
  }

  logout(): void {
    const token = this.getAccessToken();
    if (token) {
      // Fire-and-forget; even if the server call fails the local session ends.
      this.http.post(`${this.baseUrl}/logout`, {}).subscribe({ error: () => void 0 });
    }
    this.clearSession();
    this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_KEY);
  }

  /**
   * Called by the HTTP interceptor after a 401 retry fails. Keeps the UX
   * quiet and routes back to login without triggering another /logout call.
   */
  forceLogout(): void {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  private persistTokens(res: AuthResponse): void {
    localStorage.setItem(ACCESS_KEY, res.accessToken);
    localStorage.setItem(REFRESH_KEY, res.refreshToken);
    this._currentUser.set(decodeUser(res.accessToken));
  }

  private clearSession(): void {
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    this._currentUser.set(null);
  }

  private readInitialUser(): CurrentUser | null {
    const token = localStorage.getItem(ACCESS_KEY);
    if (!token) return null;
    try {
      const payload = decodePayload(token);
      const exp = payload['exp'];
      if (typeof exp === 'number' && exp * 1000 <= Date.now()) {
        return null;
      }
      return userFromPayload(payload);
    } catch {
      return null;
    }
  }
}

function decodeUser(accessToken: string): CurrentUser {
  return userFromPayload(decodePayload(accessToken));
}

function decodePayload(token: string): Record<string, unknown> {
  const parts = token.split('.');
  if (parts.length !== 3) throw new Error('Malformed JWT');
  const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64 + '=='.slice(0, (4 - (base64.length % 4)) % 4);
  const json = atob(padded);
  return JSON.parse(json) as Record<string, unknown>;
}

function userFromPayload(payload: Record<string, unknown>): CurrentUser {
  const rawRoles = payload['role'];
  const roles = Array.isArray(rawRoles)
    ? rawRoles.map(String)
    : typeof rawRoles === 'string'
      ? [rawRoles]
      : [];

  return {
    id: String(payload['sub'] ?? ''),
    email: String(payload['email'] ?? ''),
    firstName: String(payload['firstName'] ?? ''),
    lastName: String(payload['lastName'] ?? ''),
    roles
  };
}
