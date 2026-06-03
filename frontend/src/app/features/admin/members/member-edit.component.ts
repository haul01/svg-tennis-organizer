import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { format } from 'date-fns';
import { de } from 'date-fns/locale';
import { firstValueFrom } from 'rxjs';

import { MembersApi } from '../../../core/api/members.api';
import { MEMBER_ROLES, MemberDetailDto, MemberRole } from '../../../core/models/member.model';
import { ApiError, ValidationFailure } from '../../../core/models/result.model';
import {
  ConfirmDialogComponent,
  ConfirmDialogData
} from '../../../shared/components/confirm-dialog.component';

@Component({
  selector: 'app-member-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSlideToggleModule
  ],
  templateUrl: './member-edit.component.html',
  styleUrl: './member-edit.component.scss'
})
export class MemberEditComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MembersApi);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly id = input.required<string>();

  readonly member = signal<MemberDetailDto | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly failures = signal<ValidationFailure[]>([]);

  // Include every role (Guest too) so a self-registered member's current
  // role renders in the select instead of showing blank and submitting an
  // unknown value the backend rejects. Role promotion/demotion is still meant
  // to flow through the dedicated /role endpoint (last-admin check +
  // refresh-token revocation) via the arrow buttons in the members list.
  readonly availableRoles = MEMBER_ROLES;

  readonly createdAtLabel = computed(() => {
    const m = this.member();
    return m ? format(new Date(m.createdAt), 'd. MMMM yyyy', { locale: de }) : '';
  });

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    role: ['Member' as MemberRole, Validators.required]
  });

  constructor() {
    queueMicrotask(() => void this.load());
  }

  async save(): Promise<void> {
    const current = this.member();
    if (!current || this.saving()) return;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.saving.set(true);
    this.errorMessage.set(null);
    this.failures.set([]);
    try {
      const updated = await firstValueFrom(this.api.update(current.id, this.form.getRawValue()));
      this.member.set(updated);
      this.form.markAsPristine();
      this.snackBar.open('Änderungen gespeichert.', 'OK', { duration: 3000 });
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 400) {
        const body = err.error as ApiError | undefined;
        this.errorMessage.set(body?.error ?? 'Änderungen konnten nicht gespeichert werden.');
        this.failures.set(body?.failures ?? []);
      } else {
        this.errorMessage.set('Änderungen konnten nicht gespeichert werden.');
      }
    } finally {
      this.saving.set(false);
    }
  }

  async toggleActive(nextValue: boolean): Promise<void> {
    const current = this.member();
    if (!current) return;

    // Confirm the destructive direction.
    if (!nextValue) {
      const confirmed = await firstValueFrom(this.dialog.open<
        ConfirmDialogComponent, ConfirmDialogData, boolean
      >(ConfirmDialogComponent, {
        data: {
          title: 'Mitglied deaktivieren?',
          message: `${current.firstName} ${current.lastName} kann sich danach nicht mehr anmelden. Bestehende Buchungen bleiben erhalten.`,
          confirmLabel: 'Deaktivieren',
          cancelLabel: 'Abbrechen',
          destructive: true
        },
        width: '440px'
      }).afterClosed());
      if (!confirmed) return;
    }

    try {
      const updated = await firstValueFrom(this.api.setActive(current.id, nextValue));
      this.member.set(updated);
      this.snackBar.open(
        nextValue ? 'Mitglied aktiviert.' : 'Mitglied deaktiviert.',
        'OK',
        { duration: 3000 }
      );
    } catch (err) {
      const msg = (err instanceof HttpErrorResponse)
        ? (err.error as ApiError | undefined)?.error ?? 'Statusänderung nicht möglich.'
        : 'Statusänderung nicht möglich.';
      this.snackBar.open(msg, 'OK', { duration: 5000 });
    }
  }

  async triggerPasswordReset(): Promise<void> {
    const current = this.member();
    if (!current) return;

    const confirmed = await firstValueFrom(this.dialog.open<
      ConfirmDialogComponent, ConfirmDialogData, boolean
    >(ConfirmDialogComponent, {
      data: {
        title: 'Passwort zurücksetzen?',
        message: `${current.firstName} ${current.lastName} erhält eine E-Mail mit einem neuen Passwort-Setzen-Link. Das aktuelle Passwort wird nicht gelöscht, bleibt aber bis zum Setzen des neuen Passworts gültig.`,
        confirmLabel: 'Reset auslösen',
        cancelLabel: 'Abbrechen'
      },
      width: '440px'
    }).afterClosed());
    if (!confirmed) return;

    try {
      await firstValueFrom(this.api.triggerPasswordReset(current.id));
      this.snackBar.open('Reset-Mail verschickt.', 'OK', { duration: 3000 });
    } catch (err) {
      const msg = (err instanceof HttpErrorResponse)
        ? (err.error as ApiError | undefined)?.error ?? 'Reset fehlgeschlagen.'
        : 'Reset fehlgeschlagen.';
      this.snackBar.open(msg, 'OK', { duration: 5000 });
    }
  }

  back(): void {
    this.router.navigate(['/admin/members']);
  }

  private async load(): Promise<void> {
    try {
      const m = await firstValueFrom(this.api.get(this.id()));
      this.member.set(m);
      this.form.reset({
        firstName: m.firstName,
        lastName: m.lastName,
        email: m.email,
        role: m.role
      });
    } catch {
      this.errorMessage.set('Mitglied konnte nicht geladen werden.');
    } finally {
      this.loading.set(false);
    }
  }
}
