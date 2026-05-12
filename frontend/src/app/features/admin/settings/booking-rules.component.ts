import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { firstValueFrom } from 'rxjs';

import { SettingsApi } from '../../../core/api/settings.api';
import { ApiError, ValidationFailure } from '../../../core/models/result.model';

@Component({
  selector: 'app-booking-rules',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './booking-rules.component.html',
  styleUrl: './booking-rules.component.scss'
})
export class BookingRulesComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(SettingsApi);
  private readonly snackBar = inject(MatSnackBar);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly failures = signal<ValidationFailure[]>([]);

  readonly form = this.fb.nonNullable.group({
    maxAdvanceBookingDays: [7, [Validators.required, Validators.min(1), Validators.max(365)]],
    minCancellationHours: [0, [Validators.required, Validators.min(0), Validators.max(168)]],
    maxOpenReservationsPerMember: [2, [Validators.required, Validators.min(1), Validators.max(20)]],
    maxSlotsPerBooking: [4, [Validators.required, Validators.min(1), Validators.max(8)]]
  });

  constructor() {
    void this.load();
  }

  async save(): Promise<void> {
    if (this.saving()) return;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.errorMessage.set(null);
    this.failures.set([]);

    try {
      const updated = await firstValueFrom(this.api.update(this.form.getRawValue()));
      this.form.reset(updated);
      this.snackBar.open('Buchungsregeln gespeichert.', 'OK', { duration: 3000 });
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 400) {
        const body = err.error as ApiError | undefined;
        this.errorMessage.set(body?.error ?? 'Speichern fehlgeschlagen.');
        this.failures.set(body?.failures ?? []);
      } else {
        this.errorMessage.set('Speichern fehlgeschlagen.');
      }
    } finally {
      this.saving.set(false);
    }
  }

  private async load(): Promise<void> {
    try {
      const settings = await firstValueFrom(this.api.getPublic());
      this.form.reset(settings);
    } catch {
      this.errorMessage.set('Regeln konnten nicht geladen werden.');
    } finally {
      this.loading.set(false);
    }
  }
}
