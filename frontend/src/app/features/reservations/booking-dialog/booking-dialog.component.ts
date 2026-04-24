import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef
} from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { format } from 'date-fns';
import { de } from 'date-fns/locale';

import { ValidationFailure } from '../../../core/models/result.model';
import { ReservationsService } from '../reservations.service';

export interface BookingDialogData {
  courtId: number;
  courtName: string;
  startsAt: Date;
  endsAt: Date;
}

export type BookingDialogResult = { ok: true; id: string } | null;

@Component({
  selector: 'app-booking-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule
  ],
  templateUrl: './booking-dialog.component.html',
  styleUrl: './booking-dialog.component.scss'
})
export class BookingDialogComponent {
  private readonly reservations = inject(ReservationsService);
  private readonly dialogRef = inject(MatDialogRef<BookingDialogComponent, BookingDialogResult>);

  readonly data = inject<BookingDialogData>(MAT_DIALOG_DATA);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly ruleFailures = signal<ValidationFailure[]>([]);
  readonly hasGuest = signal(false);

  readonly dateLabel = computed(() =>
    format(this.data.startsAt, "EEEE, d. MMMM yyyy", { locale: de }));

  readonly timeRangeLabel = computed(() =>
    `${format(this.data.startsAt, 'HH:mm')} – ${format(this.data.endsAt, 'HH:mm')}`);

  onGuestToggle(enabled: boolean): void {
    this.hasGuest.set(enabled);
  }

  cancel(): void {
    this.dialogRef.close(null);
  }

  async submit(): Promise<void> {
    if (this.submitting()) return;
    this.errorMessage.set(null);
    this.ruleFailures.set([]);
    this.submitting.set(true);

    // Guest name collection is deferred; the toggle only surfaces the fee
    // reminder for now. GuestPlayerId stays null until that flow ships.
    const result = await this.reservations.create({
      courtId: this.data.courtId,
      startsAt: this.data.startsAt.toISOString(),
      endsAt: this.data.endsAt.toISOString(),
      guestPlayerId: null
    });
    this.submitting.set(false);

    if (result.ok) {
      this.dialogRef.close({ ok: true, id: result.id });
      return;
    }

    switch (result.status) {
      case 'conflict':
        this.errorMessage.set(result.message);
        break;
      case 'invalid':
        this.errorMessage.set(result.message ?? 'Buchung kann nicht angelegt werden.');
        this.ruleFailures.set(result.failures ?? []);
        break;
      default:
        this.errorMessage.set(result.message);
    }
  }
}
