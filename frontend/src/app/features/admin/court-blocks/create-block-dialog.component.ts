import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import {
  MAT_DIALOG_DATA,
  MatDialog,
  MatDialogModule,
  MatDialogRef
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { firstValueFrom } from 'rxjs';

import { CourtBlocksApi } from '../../../core/api/court-blocks.api';
import { CourtDto } from '../../../core/models/court.model';
import { ApiError, ValidationFailure } from '../../../core/models/result.model';
import {
  ConfirmDialogComponent,
  ConfirmDialogData
} from '../../../shared/components/confirm-dialog.component';

export interface CreateBlockDialogData {
  courts: CourtDto[];
  defaultEndDate: string; // YYYY-MM-DD - season end; reasonable default for weekly
}

@Component({
  selector: 'app-create-block-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatRadioModule,
    MatSelectModule
  ],
  templateUrl: './create-block-dialog.component.html',
  styleUrl: './create-block-dialog.component.scss'
})
export class CreateBlockDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CourtBlocksApi);
  private readonly dialog = inject(MatDialog);
  private readonly ref = inject(MatDialogRef<CreateBlockDialogComponent, boolean>);
  readonly data = inject<CreateBlockDialogData>(MAT_DIALOG_DATA);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly failures = signal<ValidationFailure[]>([]);
  readonly mode = signal<'once' | 'series'>('once');

  readonly weekdays = [
    { value: 1, label: 'Montag' },
    { value: 2, label: 'Dienstag' },
    { value: 3, label: 'Mittwoch' },
    { value: 4, label: 'Donnerstag' },
    { value: 5, label: 'Freitag' },
    { value: 6, label: 'Samstag' },
    { value: 0, label: 'Sonntag' }
  ];

  readonly form = this.fb.nonNullable.group({
    courtId: [this.data.courts[0]?.id ?? 0, Validators.required],
    date: this.fb.control<Date | null>(null, Validators.required),
    startTime: ['08:00', Validators.required],
    endTime: ['09:00', Validators.required],
    reason: ['', [Validators.required, Validators.maxLength(200)]],
    weekday: [1 as number, Validators.required],
    rangeStart: this.fb.control<Date | null>(null),
    rangeEnd: this.fb.control<Date | null>(this.data.defaultEndDate ? fromIsoDate(this.data.defaultEndDate) : null)
  });

  setMode(mode: 'once' | 'series'): void {
    this.mode.set(mode);
    this.errorMessage.set(null);
    this.failures.set([]);
  }

  cancel(): void { this.ref.close(false); }

  async submit(): Promise<void> {
    if (this.submitting()) return;
    this.errorMessage.set(null);
    this.failures.set([]);

    const mode = this.mode();
    const raw = this.form.getRawValue();

    if (mode === 'once') {
      if (!raw.date || !raw.courtId || !raw.reason.trim()) {
        this.form.markAllAsTouched();
        return;
      }
      await this.createOnce(false);
    } else {
      if (!raw.rangeStart || !raw.rangeEnd || !raw.courtId || !raw.reason.trim()) {
        this.form.markAllAsTouched();
        return;
      }
      await this.createSeries(false);
    }
  }

  private async createOnce(forceCancelConflicts: boolean): Promise<void> {
    const raw = this.form.getRawValue();
    const { startsAt, endsAt } = joinDateAndTimes(raw.date!, raw.startTime, raw.endTime);

    this.submitting.set(true);
    try {
      const response = await firstValueFrom(this.api.createOnce({
        courtId: raw.courtId,
        startsAt: startsAt.toISOString(),
        endsAt: endsAt.toISOString(),
        reason: raw.reason.trim(),
        forceCancelConflicts
      }));
      if (response.cancelledReservations > 0) {
        // Informational for the admin; UI continues to close.
        console.info(`${response.cancelledReservations} Buchungen storniert.`);
      }
      this.ref.close(true);
    } catch (err) {
      await this.handleCreateError(err, () => this.createOnce(true));
    } finally {
      this.submitting.set(false);
    }
  }

  private async createSeries(forceCancelConflicts: boolean): Promise<void> {
    const raw = this.form.getRawValue();

    this.submitting.set(true);
    try {
      await firstValueFrom(this.api.createSeries({
        courtId: raw.courtId,
        weekday: raw.weekday as 0 | 1 | 2 | 3 | 4 | 5 | 6,
        startTime: padTime(raw.startTime),
        endTime: padTime(raw.endTime),
        startDate: toIsoDate(raw.rangeStart!),
        endDate: toIsoDate(raw.rangeEnd!),
        reason: raw.reason.trim(),
        forceCancelConflicts
      }));
      this.ref.close(true);
    } catch (err) {
      await this.handleCreateError(err, () => this.createSeries(true));
    } finally {
      this.submitting.set(false);
    }
  }

  private async handleCreateError(err: unknown, retry: () => Promise<void>): Promise<void> {
    if (err instanceof HttpErrorResponse && err.status === 409) {
      const body = err.error as ApiError | undefined;
      const confirmed = await firstValueFrom(this.dialog.open<
        ConfirmDialogComponent, ConfirmDialogData, boolean
      >(ConfirmDialogComponent, {
        data: {
          title: 'Bestehende Buchungen stornieren?',
          message: (body?.error ?? 'Buchungen überschneiden sich mit dem Zeitraum.')
            + ' Sollen diese jetzt storniert werden, damit die Sperre wirksam wird?',
          confirmLabel: 'Buchungen stornieren & sperren',
          cancelLabel: 'Abbrechen',
          destructive: true
        },
        width: '480px'
      }).afterClosed());
      if (confirmed) await retry();
      return;
    }

    if (err instanceof HttpErrorResponse && err.status === 400) {
      const body = err.error as ApiError | undefined;
      this.errorMessage.set(body?.error ?? 'Sperre konnte nicht angelegt werden.');
      this.failures.set(body?.failures ?? []);
    } else {
      this.errorMessage.set('Sperre konnte nicht angelegt werden.');
    }
  }
}

function joinDateAndTimes(date: Date, startTime: string, endTime: string): { startsAt: Date; endsAt: Date } {
  const [sh, sm] = startTime.split(':').map(Number);
  const [eh, em] = endTime.split(':').map(Number);
  const startsAt = new Date(date);
  startsAt.setHours(sh, sm, 0, 0);
  const endsAt = new Date(date);
  endsAt.setHours(eh, em, 0, 0);
  return { startsAt, endsAt };
}

function toIsoDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function fromIsoDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(y, m - 1, d);
}

function padTime(hhmm: string): string {
  return hhmm.length === 5 ? `${hhmm}:00` : hhmm;
}
