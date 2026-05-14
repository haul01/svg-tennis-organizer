import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-register',
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
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]]
  });

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  // Switches the view to the "check your inbox" confirmation panel.
  readonly submitted = signal(false);
  readonly currentYear = new Date().getFullYear();

  submit(): void {
    if (this.submitting() || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const { email, firstName, lastName } = this.form.getRawValue();
    this.auth.register(email.trim(), firstName.trim(), lastName.trim()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.submitted.set(true);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        if (err.status === 429) {
          this.errorMessage.set(
            'Zu viele Registrierungsversuche. Bitte versuche es später erneut.');
        } else if (err.status === 400) {
          // Validator-level error - show the server's hint if any.
          this.errorMessage.set(
            err.error?.errors
              ? 'Bitte prüfe deine Angaben.'
              : (err.error?.error ?? 'Registrierung fehlgeschlagen.'));
        } else {
          this.errorMessage.set(
            'Registrierung gerade nicht möglich. Bitte später erneut versuchen.');
        }
      }
    });
  }
}
