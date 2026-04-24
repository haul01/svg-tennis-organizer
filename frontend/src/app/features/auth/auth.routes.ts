import { Routes } from '@angular/router';

import { PlaceholderComponent } from '../../shared/components/placeholder.component';

export const authRoutes: Routes = [
  { path: 'login', component: PlaceholderComponent, title: 'Anmelden' },
  { path: 'forgot-password', component: PlaceholderComponent, title: 'Passwort vergessen' },
  { path: 'set-password', component: PlaceholderComponent, title: 'Passwort setzen' }
];
