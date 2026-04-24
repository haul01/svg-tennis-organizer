import { Routes } from '@angular/router';

export const profileRoutes: Routes = [
  {
    path: '',
    title: 'Profil',
    loadComponent: () => import('./profile.component').then(m => m.ProfileComponent)
  }
];
