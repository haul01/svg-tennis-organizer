import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, Input, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-set-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './set-password.component.html',
  styleUrl: './set-password.component.scss'
})
export class SetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  // withComponentInputBinding(): query string params land here as @Input.
  @Input() email = '';
  @Input() token = '';

  readonly form = this.fb.nonNullable.group(
    {
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: passwordsMatchValidator }
  );

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly newPwVisible = signal(false);
  readonly confirmPwVisible = signal(false);
  readonly currentYear = new Date().getFullYear();

  readonly missingTokenOrEmail = computed(() => !this.email || !this.token);

  toggleNewPwVisibility(): void {
    this.newPwVisible.update(v => !v);
  }

  toggleConfirmPwVisibility(): void {
    this.confirmPwVisible.update(v => !v);
  }

  async submit(): Promise<void> {
    if (this.submitting() || this.form.invalid || this.missingTokenOrEmail()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const { newPassword } = this.form.getRawValue();
    this.auth.resetPassword(this.email, this.token, newPassword).subscribe({
      next: () => {
        this.submitting.set(false);
        this.snackBar.open('Passwort gesetzt. Du kannst dich jetzt anmelden.', 'OK', {
          duration: 5000
        });
        this.router.navigate(['/login']);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        const fallback = 'Der Link ist ungültig oder abgelaufen. Bitte fordere einen neuen an.';
        this.errorMessage.set(extractServerMessage(err) ?? fallback);
      }
    });
  }
}

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const a = group.get('newPassword')?.value;
  const b = group.get('confirmPassword')?.value;
  return a && b && a !== b ? { passwordsMismatch: true } : null;
}

/**
 * The backend returns two different 400 shapes: the Result pattern
 * ({ error, failures }) for handler/business failures, and ASP.NET's
 * ProblemDetails ({ errors: { field: [msg] } }) for FluentValidation
 * failures (e.g. password too short). Read both so a password-policy error
 * surfaces as itself instead of the misleading "link expired" fallback.
 */
function extractServerMessage(err: HttpErrorResponse): string | undefined {
  const body = err.error as
    | { error?: string; failures?: { message: string }[]; errors?: Record<string, string[]> }
    | undefined;
  if (!body) return undefined;
  if (body.error) return body.error;
  if (body.failures?.length) return body.failures[0].message;
  const firstField = body.errors && Object.values(body.errors).find(m => m?.length);
  return firstField?.[0];
}
