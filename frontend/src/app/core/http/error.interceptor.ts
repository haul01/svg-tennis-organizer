import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

/**
 * Placeholder error interceptor. Phase 5+ will plug in a snackbar or global
 * error surface; for now we just let errors propagate to the calling component.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((err: HttpErrorResponse) => throwError(() => err))
  );
