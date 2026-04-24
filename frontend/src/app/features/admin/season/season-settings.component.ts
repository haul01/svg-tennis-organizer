import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { firstValueFrom } from 'rxjs';

import { SeasonsApi } from '../../../core/api/seasons.api';
import { SeasonDto, UpdateSeasonRequest } from '../../../core/models/season.model';
import { ApiError, ValidationFailure } from '../../../core/models/result.model';

@Component({
  selector: 'app-season-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './season-settings.component.html',
  styleUrl: './season-settings.component.scss'
})
export class SeasonSettingsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(SeasonsApi);
  private readonly snackBar = inject(MatSnackBar);

  readonly season = signal<SeasonDto | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly failures = signal<ValidationFailure[]>([]);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    startDate: this.fb.control<Date | null>(null, Validators.required),
    endDate: this.fb.control<Date | null>(null, Validators.required),
    openingTime: ['08:00', Validators.required],
    closingTime: ['22:00', Validators.required],
    slotDurationMinutes: [60, [Validators.required, Validators.min(15), Validators.max(240)]]
  });

  constructor() {
    void this.load();
  }

  async save(): Promise<void> {
    const current = this.season();
    if (!current || this.saving()) return;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.saving.set(true);
    this.errorMessage.set(null);
    this.failures.set([]);

    const raw = this.form.getRawValue();
    const request: UpdateSeasonRequest = {
      name: raw.name,
      startDate: toIsoDate(raw.startDate!),
      endDate: toIsoDate(raw.endDate!),
      openingTime: padTime(raw.openingTime),
      closingTime: padTime(raw.closingTime),
      slotDurationMinutes: raw.slotDurationMinutes
    };

    try {
      const updated = await firstValueFrom(this.api.update(current.id, request));
      this.season.set(updated);
      this.populateFromDto(updated);
      this.snackBar.open('Saison gespeichert.', 'OK', { duration: 3000 });
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
      const s = await firstValueFrom(this.api.current());
      if (!s) {
        this.errorMessage.set('Keine aktive Saison gefunden. Anlegen einer neuen Saison folgt in einer späteren Phase.');
        return;
      }
      this.season.set(s);
      this.populateFromDto(s);
    } catch {
      this.errorMessage.set('Saison konnte nicht geladen werden.');
    } finally {
      this.loading.set(false);
    }
  }

  private populateFromDto(s: SeasonDto): void {
    this.form.reset({
      name: s.name,
      startDate: fromIsoDate(s.startDate),
      endDate: fromIsoDate(s.endDate),
      openingTime: s.openingTime.slice(0, 5),
      closingTime: s.closingTime.slice(0, 5),
      slotDurationMinutes: s.slotDurationMinutes
    });
  }
}

function fromIsoDate(iso: string): Date {
  // "YYYY-MM-DD" -> local Date at midnight.
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(y, m - 1, d);
}

function toIsoDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function padTime(hhmm: string): string {
  // Native <input type="time"> emits "HH:mm"; backend expects "HH:mm:ss".
  return hhmm.length === 5 ? `${hhmm}:00` : hhmm;
}
