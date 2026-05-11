import { Routes } from '@angular/router';

import { adminGuard, authGuard } from './core/auth/auth.guard';
import { ShellComponent } from './shared/components/shell.component';

export const routes: Routes = [
  // Without this full-match redirect, the loadChildren entry below would
  // claim '/' but its lazy children have no '' child, so the outlet
  // stayed empty. Sending '/' through the authed-shell route lets its
  // child redirect to /reservations (or the auth guard bounce to /login
  // when no session exists).
  { path: '', pathMatch: 'full', redirectTo: '/reservations' },
  {
    path: '',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.authRoutes)
  },
  {
    path: '',
    canActivate: [authGuard],
    component: ShellComponent,
    children: [
      {
        path: 'reservations',
        loadChildren: () =>
          import('./features/reservations/reservations.routes').then(m => m.reservationsRoutes)
      },
      {
        path: 'profile',
        loadChildren: () =>
          import('./features/profile/profile.routes').then(m => m.profileRoutes)
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes)
      },
      { path: '', redirectTo: 'reservations', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: '' }
];
