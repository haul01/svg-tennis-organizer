import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RouterLink } from '@angular/router';
import { debounceTime, firstValueFrom } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';

import { MembersApi } from '../../../core/api/members.api';
import {
  MEMBER_ROLES,
  MemberListItemDto,
  MemberRole
} from '../../../core/models/member.model';
import {
  ConfirmDialogComponent,
  ConfirmDialogData
} from '../../../shared/components/confirm-dialog.component';
import { CreateMemberDialogComponent } from './create-member-dialog.component';

type StatusFilter = 'all' | 'active' | 'inactive';

@Component({
  selector: 'app-members-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './members-list.component.html',
  styleUrl: './members-list.component.scss'
})
export class MembersListComponent {
  private readonly api = inject(MembersApi);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly members = signal<MemberListItemDto[]>([]);
  readonly loading = signal(true);
  readonly status = signal<StatusFilter>('all');
  readonly roleFilter = signal<MemberRole | null>(null);

  readonly searchControl = new FormControl('', { nonNullable: true });
  private readonly searchSignal = toSignal(
    this.searchControl.valueChanges.pipe(debounceTime(200)),
    { initialValue: '' }
  );

  readonly availableRoles = MEMBER_ROLES;

  readonly counts = computed(() => {
    const all = this.members();
    return {
      all: all.length,
      active: all.filter(m => m.isActive).length,
      inactive: all.filter(m => !m.isActive).length
    };
  });

  constructor() {
    void this.reload();

    // Reload on search term change (debounced).
    toSignal(
      this.searchControl.valueChanges.pipe(debounceTime(250)),
      { initialValue: '' }
    );
    this.searchControl.valueChanges
      .pipe(debounceTime(250))
      .subscribe(() => void this.reload());
  }

  setStatus(status: StatusFilter): void {
    this.status.set(status);
    void this.reload();
  }

  setRole(role: MemberRole | null): void {
    this.roleFilter.set(role);
    void this.reload();
  }

  initials(m: MemberListItemDto): string {
    return (m.firstName[0] ?? '') + (m.lastName[0] ?? '');
  }

  openCreate(): void {
    const ref = this.dialog.open<
      CreateMemberDialogComponent, void, MemberListItemDto | null
    >(CreateMemberDialogComponent, { width: '480px', autoFocus: false });
    ref.afterClosed().subscribe(created => {
      if (created) {
        this.snackBar.open(
          `Mitglied angelegt. Willkommens-Mail an ${created.email} verschickt.`,
          'OK',
          { duration: 4000 }
        );
        void this.reload();
      }
    });
  }

  async toggleActive(m: MemberListItemDto): Promise<void> {
    const nextValue = !m.isActive;
    try {
      const updated = await firstValueFrom(this.api.setActive(m.id, nextValue));
      this.members.update(list => list.map(it => it.id === updated.id ? updated : it));
      this.snackBar.open(
        nextValue ? 'Mitglied aktiviert.' : 'Mitglied deaktiviert.',
        'OK',
        { duration: 3000 }
      );
    } catch (err: unknown) {
      this.snackBar.open(
        (err as { error?: { error?: string } })?.error?.error
          ?? 'Statusänderung nicht möglich.',
        'OK',
        { duration: 5000 }
      );
    }
  }

  async promoteToMember(m: MemberListItemDto): Promise<void> {
    await this.changeRole(m, 'Member',
      `${m.firstName} ${m.lastName} zum Vollmitglied befördern?`,
      `${m.firstName} darf danach alle Plätze buchen und sieht den
       Gast-Hinweis nicht mehr.`,
      'Befördern');
  }

  async demoteToGuest(m: MemberListItemDto): Promise<void> {
    await this.changeRole(m, 'Guest',
      `${m.firstName} ${m.lastName} auf Gast zurückstufen?`,
      `${m.firstName} darf danach nur noch für Gäste freigegebene Plätze
       buchen. Bestehende Buchungen bleiben erhalten.`,
      'Zurückstufen');
  }

  private async changeRole(
    m: MemberListItemDto,
    role: MemberRole,
    title: string,
    message: string,
    confirmLabel: string
  ): Promise<void> {
    const confirmed = await firstValueFrom(this.dialog.open<
      ConfirmDialogComponent, ConfirmDialogData, boolean
    >(ConfirmDialogComponent, {
      data: { title, message, confirmLabel, cancelLabel: 'Abbrechen' },
      width: '440px',
      maxWidth: '95vw'
    }).afterClosed());

    if (!confirmed) return;

    try {
      const updated = await firstValueFrom(this.api.changeRole(m.id, role));
      this.members.update(list => list.map(it => it.id === updated.id ? updated : it));
      this.snackBar.open(`Rolle geändert: ${updated.role}.`, 'OK', { duration: 3000 });
    } catch (err: unknown) {
      this.snackBar.open(
        (err as { error?: { error?: string } })?.error?.error
          ?? 'Rollenwechsel nicht möglich.',
        'OK',
        { duration: 6000 }
      );
    }
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    try {
      const status = this.status();
      const list = await firstValueFrom(this.api.list({
        search: this.searchSignal() || undefined,
        status: status === 'all' ? undefined : status,
        role: this.roleFilter() ?? undefined
      }));
      this.members.set(list);
    } finally {
      this.loading.set(false);
    }
  }
}
