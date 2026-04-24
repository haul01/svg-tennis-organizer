import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { firstValueFrom } from 'rxjs';

import { ProfileApi } from '../../core/api/profile.api';
import { AuthService } from '../../core/auth/auth.service';
import { ProfileDto } from '../../core/models/profile.model';
import { ApiError, ValidationFailure } from '../../core/models/result.model';
import { ChangePasswordDialogComponent } from './change-password-dialog.component';

@Component({
  selector: 'app-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ProfileApi);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly profile = signal<ProfileDto | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly failures = signal<ValidationFailure[]>([]);

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]]
  });

  constructor() {
    void this.load();
  }

  async save(): Promise<void> {
    if (this.saving()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorMessage.set(null);
    this.failures.set([]);

    try {
      const updated = await firstValueFrom(this.api.update(this.form.getRawValue()));
      this.profile.set(updated);
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

  revertChanges(): void {
    const p = this.profile();
    if (!p) return;
    this.form.reset({ firstName: p.firstName, lastName: p.lastName });
    this.errorMessage.set(null);
    this.failures.set([]);
  }

  openPasswordDialog(): void {
    const ref = this.dialog.open<ChangePasswordDialogComponent, void, boolean>(
      ChangePasswordDialogComponent,
      { width: '480px', autoFocus: false }
    );
    ref.afterClosed().subscribe(changed => {
      if (changed) {
        this.snackBar.open('Passwort geändert.', 'OK', { duration: 3000 });
      }
    });
  }

  logout(): void {
    this.auth.logout();
  }

  private async load(): Promise<void> {
    try {
      const p = await firstValueFrom(this.api.get());
      this.profile.set(p);
      this.form.reset({ firstName: p.firstName, lastName: p.lastName });
    } catch {
      this.errorMessage.set('Profil konnte nicht geladen werden.');
    } finally {
      this.loading.set(false);
    }
  }
}
