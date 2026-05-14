import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
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

import { AuthService } from '../../../core/auth/auth.service';
import { ValidationFailure } from '../../../core/models/result.model';
import { ReservationsService } from '../reservations.service';

export interface BookingDialogData {
  courtId: number;
  courtName: string;
  startsAt: Date;
  slotMinutes: number;
  maxSlots: number;
  /**
   * Admin-configured nudge shown only to Guest-role bookers. Empty
   * string disables the prompt entirely.
   */
  guestMembershipPromptText: string;
}

export type BookingDialogResult = { ok: true; id: string } | null;

interface DurationOption {
  slots: number;
  label: string;
}

@Component({
  selector: 'app-booking-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatButtonToggleModule,
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
  private readonly auth = inject(AuthService);
  private readonly dialogRef = inject(MatDialogRef<BookingDialogComponent, BookingDialogResult>);

  readonly data = inject<BookingDialogData>(MAT_DIALOG_DATA);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly ruleFailures = signal<ValidationFailure[]>([]);
  readonly hasGuest = signal(false);

  // Show the membership prompt only when (a) the current session is a
  // Guest and (b) the admin has actually configured prompt text.
  readonly showMembershipPrompt = computed(() => {
    const user = this.auth.currentUser();
    const isGuest = user?.roles.includes('Guest') ?? false;
    return isGuest && this.data.guestMembershipPromptText.trim().length > 0;
  });

  // Default to ~2 h (4 slots for 30 min, 2 slots for 60 min); never
  // exceed the admin-configured cap.
  readonly slotsCount = signal<number>(
    Math.min(this.data.maxSlots, Math.max(1, Math.round(120 / this.data.slotMinutes)))
  );

  readonly durationOptions = computed<DurationOption[]>(() => {
    const slot = this.data.slotMinutes;
    const max = this.data.maxSlots;
    // Anything under 1 h is too short for a typical match - start the
    // pick list at whatever slot count brings us to 60 min.
    const min = Math.max(1, Math.round(60 / slot));
    const opts: DurationOption[] = [];
    for (let n = min; n <= max; n++) {
      opts.push({ slots: n, label: formatDuration(n * slot) });
    }
    return opts;
  });

  readonly endsAt = computed(() =>
    new Date(this.data.startsAt.getTime() + this.slotsCount() * this.data.slotMinutes * 60_000));

  readonly dateLabel = computed(() =>
    format(this.data.startsAt, "EEEE, d. MMMM yyyy", { locale: de }));

  readonly timeRangeLabel = computed(() =>
    `${format(this.data.startsAt, 'HH:mm')} – ${format(this.endsAt(), 'HH:mm')}`);

  setSlots(slots: number): void {
    this.slotsCount.set(slots);
  }

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

    const result = await this.reservations.create({
      courtId: this.data.courtId,
      startsAt: this.data.startsAt.toISOString(),
      endsAt: this.endsAt().toISOString(),
      guestPlayerId: null,
      hasGuest: this.hasGuest()
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

function formatDuration(minutes: number): string {
  if (minutes < 60) return `${minutes} min`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return m === 0 ? `${h} h` : `${h} h ${m} min`;
}
