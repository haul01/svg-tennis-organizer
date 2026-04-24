import { Routes } from '@angular/router';

import { PlaceholderComponent } from '../../shared/components/placeholder.component';

export const adminRoutes: Routes = [
  { path: '', component: PlaceholderComponent, title: 'Admin' },
  { path: 'members', component: PlaceholderComponent, title: 'Mitglieder' },
  { path: 'courts', component: PlaceholderComponent, title: 'Plätze' },
  { path: 'court-blocks', component: PlaceholderComponent, title: 'Platzsperren' },
  { path: 'season', component: PlaceholderComponent, title: 'Saison' },
  { path: 'settings', component: PlaceholderComponent, title: 'Buchungsregeln' },
  { path: 'guest-billing', component: PlaceholderComponent, title: 'Gastspieler-Abrechnung' }
];
