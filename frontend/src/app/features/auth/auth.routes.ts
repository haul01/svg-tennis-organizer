import { Routes } from '@angular/router';

import { PlaceholderComponent } from '../../shared/components/placeholder.component';

export const authRoutes: Routes = [
  {
    path: 'login',
    title: 'Anmelden',
    loadComponent: () => import('./login/login.component').then(m => m.LoginComponent)
  },
  { path: 'forgot-password', component: PlaceholderComponent, title: 'Passwort vergessen' },
  { path: 'set-password', component: PlaceholderComponent, title: 'Passwort setzen' }
];
