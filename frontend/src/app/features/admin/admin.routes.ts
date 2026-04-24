import { Routes } from '@angular/router';

import { PlaceholderComponent } from '../../shared/components/placeholder.component';

export const adminRoutes: Routes = [
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
  { path: 'courts', component: PlaceholderComponent, title: 'Plätze' },
  { path: 'court-blocks', component: PlaceholderComponent, title: 'Platzsperren' },
  { path: 'season', component: PlaceholderComponent, title: 'Saison' },
  { path: 'settings', component: PlaceholderComponent, title: 'Buchungsregeln' },
  { path: 'guest-billing', component: PlaceholderComponent, title: 'Gastspieler-Abrechnung' }
];
