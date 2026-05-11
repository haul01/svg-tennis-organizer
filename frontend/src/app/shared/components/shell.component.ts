import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatToolbarModule,
    MatTooltipModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet
  ],
  template: `
    <mat-toolbar color="primary">
      <a routerLink="/reservations" class="brand" aria-label="Startseite">
        <img src="assets/logo.png" alt="SV Gramastetten" class="brand__logo" />
      </a>
      <nav class="nav">
        <a mat-button routerLink="/reservations" routerLinkActive="active">Platzbelegung</a>
        <a mat-button routerLink="/reservations/mine" routerLinkActive="active">Meine Buchungen</a>
        <a mat-button routerLink="/profile" routerLinkActive="active">Profil</a>
        @if (auth.isAdmin()) {
          <a mat-button routerLink="/admin" routerLinkActive="active">Admin</a>
        }
      </nav>
      <span class="spacer"></span>
      @if (auth.currentUser(); as user) {
        <a
          mat-icon-button
          routerLink="/profile"
          aria-label="Profil"
          [matTooltip]="user.firstName + ' ' + user.lastName"
        >
          <mat-icon fontSet="material-symbols-outlined">account_circle</mat-icon>
        </a>
      }
      <button
        mat-icon-button
        (click)="auth.logout()"
        aria-label="Abmelden"
        matTooltip="Abmelden"
      >
        <mat-icon fontSet="material-symbols-outlined">logout</mat-icon>
      </button>
    </mat-toolbar>
    <main class="content">
      <router-outlet />
    </main>
  `,
  styles: `
    :host { display: flex; flex-direction: column; min-height: 100vh; }
    .brand { display: inline-flex; align-items: center; margin-right: 2rem; }
    .brand__logo { height: 36px; width: auto; display: block; }
    .nav { display: flex; gap: 0.5rem; }
    .nav a { color: inherit; }
    .nav .active { font-weight: 600; }
    .spacer { flex: 1; }
    .content { flex: 1; padding: 1.5rem; }
  `
})
export class ShellComponent {
  readonly auth = inject(AuthService);
}
