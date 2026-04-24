import { Routes } from '@angular/router';

export const reservationsRoutes: Routes = [
  {
    path: '',
    title: 'Platzbelegung',
    loadComponent: () =>
      import('./week-grid/week-grid.component').then(m => m.WeekGridComponent)
  },
  {
    path: 'mine',
    title: 'Meine Buchungen',
    loadComponent: () =>
      import('./my-reservations/my-reservations.component').then(m => m.MyReservationsComponent)
  }
];
