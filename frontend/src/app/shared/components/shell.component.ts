import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet
  ],
  template: `
    <mat-toolbar color="primary">
      <span class="brand">TennisClub</span>
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
        <span class="user">{{ user.firstName }} {{ user.lastName }}</span>
      }
      <button mat-icon-button aria-label="Abmelden" (click)="auth.logout()">
        <mat-icon>logout</mat-icon>
      </button>
    </mat-toolbar>
    <main class="content">
      <router-outlet />
    </main>
  `,
  styles: `
    :host { display: flex; flex-direction: column; min-height: 100vh; }
    .brand { font-weight: 600; margin-right: 2rem; }
    .nav { display: flex; gap: 0.5rem; }
    .nav .active { font-weight: 600; }
    .spacer { flex: 1; }
    .user { margin-right: 0.5rem; opacity: 0.85; }
    .content { flex: 1; padding: 1.5rem; }
  `
})
export class ShellComponent {
  readonly auth = inject(AuthService);
}
