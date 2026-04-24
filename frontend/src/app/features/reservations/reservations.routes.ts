import { Routes } from '@angular/router';

import { PlaceholderComponent } from '../../shared/components/placeholder.component';

export const reservationsRoutes: Routes = [
  { path: '', component: PlaceholderComponent, title: 'Platzbelegung' },
  { path: 'mine', component: PlaceholderComponent, title: 'Meine Buchungen' }
];
