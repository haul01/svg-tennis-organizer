import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';

import { ProfileApi } from '../../core/api/profile.api';
import { ApiError, ValidationFailure } from '../../core/models/result.model';

@Component({
  selector: 'app-change-password-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './change-password-dialog.component.html',
  styleUrl: './change-password-dialog.component.scss'
})
export class ChangePasswordDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ProfileApi);
  private readonly dialogRef = inject(MatDialogRef<ChangePasswordDialogComponent, boolean>);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly failures = signal<ValidationFailure[]>([]);
  readonly showCurrent = signal(false);
  readonly showNew = signal(false);

  readonly form = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required]
  }, { validators: matchPasswords });

  toggleCurrent(): void { this.showCurrent.update(v => !v); }
  toggleNew(): void { this.showNew.update(v => !v); }

  cancel(): void { this.dialogRef.close(false); }

  async submit(): Promise<void> {
    if (this.submitting()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.failures.set([]);

    const { currentPassword, newPassword } = this.form.getRawValue();
    try {
      await firstValueFrom(this.api.changePassword({ currentPassword, newPassword }));
      this.dialogRef.close(true);
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 400) {
        const body = err.error as ApiError | undefined;
        this.errorMessage.set(body?.error ?? 'Passwort konnte nicht geändert werden.');
        this.failures.set(body?.failures ?? []);
      } else {
        this.errorMessage.set('Passwort konnte nicht geändert werden.');
      }
    } finally {
      this.submitting.set(false);
    }
  }
}

function matchPasswords(group: AbstractControl): ValidationErrors | null {
  const next = group.get('newPassword')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return next && confirm && next !== confirm ? { passwordsMismatch: true } : null;
}
