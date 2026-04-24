import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

interface AdminNavItem {
  path: string;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-admin-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, MatIconModule],
  template: `
    <div class="layout">
      <aside class="side-nav" aria-label="Admin-Navigation">
        @for (item of items; track item.path) {
          <a
            [routerLink]="item.path"
            routerLinkActive="side-nav__link--active"
            [routerLinkActiveOptions]="{ exact: false }"
            class="side-nav__link"
          >
            <mat-icon fontSet="material-symbols-outlined">{{ item.icon }}</mat-icon>
            <span>{{ item.label }}</span>
          </a>
        }
      </aside>
      <section class="content">
        <router-outlet />
      </section>
    </div>
  `,
  styles: `
    @use '../../../styles/tokens' as *;

    :host { display: block; }

    .layout {
      display: grid;
      grid-template-columns: 220px 1fr;
      gap: var(--tc-space-md);
      align-items: start;

      @media (max-width: 800px) {
        grid-template-columns: 1fr;
      }
    }

    .side-nav {
      display: flex;
      flex-direction: column;
      gap: var(--tc-space-base);
      padding: var(--tc-space-sm);
      background: var(--tc-surface-container-lowest);
      border: 1px solid var(--tc-outline-variant);
      border-radius: var(--tc-radius-lg);
      position: sticky;
      top: var(--tc-space-md);

      @media (max-width: 800px) {
        position: static;
        flex-direction: row;
        overflow-x: auto;
      }

      &__link {
        display: inline-flex;
        align-items: center;
        gap: var(--tc-space-xs);
        padding: var(--tc-space-xs) var(--tc-space-sm);
        border-radius: var(--tc-radius);
        color: var(--tc-on-surface-variant);
        text-decoration: none;
        @include tc-label-bold;
        white-space: nowrap;
        transition: background-color 120ms ease, color 120ms ease;

        &:hover {
          background: var(--tc-surface-container-low);
          color: var(--tc-on-surface);
        }

        &--active {
          background: var(--tc-deep-navy);
          color: var(--tc-surface-container-lowest);
        }

        mat-icon {
          font-size: 20px;
          width: 20px;
          height: 20px;
        }
      }
    }

    .content {
      min-width: 0;
    }
  `
})
export class AdminShellComponent {
  readonly items: AdminNavItem[] = [
    { path: '/admin/members', label: 'Mitglieder', icon: 'group' },
    { path: '/admin/courts', label: 'Plätze', icon: 'sports_tennis' },
    { path: '/admin/court-blocks', label: 'Platzsperren', icon: 'block' },
    { path: '/admin/season', label: 'Saison', icon: 'calendar_month' },
    { path: '/admin/settings', label: 'Buchungsregeln', icon: 'tune' },
    { path: '/admin/guest-billing', label: 'Gastspieler', icon: 'receipt_long' }
  ];
}
