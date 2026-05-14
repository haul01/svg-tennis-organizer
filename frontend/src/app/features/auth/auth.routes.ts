import { Routes } from '@angular/router';

import { PlaceholderComponent } from '../../shared/components/placeholder.component';

export const authRoutes: Routes = [
  {
    path: 'login',
    title: 'Anmelden',
    loadComponent: () => import('./login/login.component').then(m => m.LoginComponent)
  },
  { path: 'forgot-password', component: PlaceholderComponent, title: 'Passwort vergessen' },
  {
    path: 'register',
    title: 'Als Gast registrieren',
    loadComponent: () => import('./register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'set-password',
    title: 'Passwort setzen',
    loadComponent: () => import('./set-password/set-password.component').then(m => m.SetPasswordComponent)
  }
];
