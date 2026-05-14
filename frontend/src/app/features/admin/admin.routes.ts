import { Routes } from '@angular/router';

import { AdminShellComponent } from './admin-shell.component';

export const adminRoutes: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    children: [
      // Until the dashboard ships (phase 7, last slice), land the admin on
      // the members list - that's the most used view.
      { path: '', redirectTo: 'members', pathMatch: 'full' },
      {
        path: 'members',
        title: 'Mitglieder',
        loadComponent: () =>
          import('./members/members-list.component').then(m => m.MembersListComponent)
      },
      {
        path: 'members/:id',
        title: 'Mitglied bearbeiten',
        loadComponent: () =>
          import('./members/member-edit.component').then(m => m.MemberEditComponent)
      },
      {
        path: 'courts',
        title: 'Plätze',
        loadComponent: () =>
          import('./courts/courts-admin.component').then(m => m.CourtsAdminComponent)
      },
      {
        path: 'court-blocks',
        title: 'Platzsperren',
        loadComponent: () =>
          import('./court-blocks/court-blocks-admin.component').then(m => m.CourtBlocksAdminComponent)
      },
      {
        path: 'season',
        title: 'Saison',
        loadComponent: () =>
          import('./season/season-settings.component').then(m => m.SeasonSettingsComponent)
      },
      {
        path: 'settings',
        title: 'Buchungsregeln',
        loadComponent: () =>
          import('./settings/booking-rules.component').then(m => m.BookingRulesComponent)
      },
      {
        path: 'reports/reservations',
        title: 'Buchungs-Report',
        loadComponent: () =>
          import('./reports/reservations-report.component').then(m => m.ReservationsReportComponent)
      }
    ]
  }
];
