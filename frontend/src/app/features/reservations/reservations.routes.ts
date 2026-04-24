import { Routes } from '@angular/router';

import { PlaceholderComponent } from '../../shared/components/placeholder.component';

export const reservationsRoutes: Routes = [
  {
    path: '',
    title: 'Platzbelegung',
    loadComponent: () =>
      import('./week-grid/week-grid.component').then(m => m.WeekGridComponent)
  },
  { path: 'mine', component: PlaceholderComponent, title: 'Meine Buchungen' }
];
