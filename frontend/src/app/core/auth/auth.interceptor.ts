import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';

import { AuthService } from './auth.service';

const AUTH_ENDPOINTS = ['/auth/login', '/auth/refresh', '/auth/forgot-password', '/auth/reset-password'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  // Let login / refresh / password flows through unauthenticated.
  if (AUTH_ENDPOINTS.some(p => req.url.includes(p))) {
    return next(req);
  }

  const token = auth.getAccessToken();
  const authed = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authed).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 401 || !token) {
        return throwError(() => err);
      }

      // Try one refresh; retry the original request with the new token.
      return auth.refresh().pipe(
        switchMap(newToken =>
          next(req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } }))
        ),
        catchError(refreshErr => {
          auth.forceLogout();
          return throwError(() => refreshErr);
        })
      );
    })
  );
};
