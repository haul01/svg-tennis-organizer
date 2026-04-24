import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { differenceInMinutes, format } from 'date-fns';
import { de } from 'date-fns/locale';
import { firstValueFrom } from 'rxjs';

import { SettingsApi } from '../../../core/api/settings.api';
import { PublicSettingsDto } from '../../../core/models/settings.model';
import {
  ConfirmDialogComponent,
  ConfirmDialogData
} from '../../../shared/components/confirm-dialog.component';
import { MyReservationDto, ReservationStatus } from '../reservation.model';
import { ReservationsService } from '../reservations.service';

interface MyReservationVm extends MyReservationDto {
  startsAtDate: Date;
  endsAtDate: Date;
  durationMinutes: number;
  isPast: boolean;
  canCancel: boolean;
  cancelBlockedReason: string | null;
}

@Component({
  selector: 'app-my-reservations',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    RouterLink
  ],
  templateUrl: './my-reservations.component.html',
  styleUrl: './my-reservations.component.scss'
})
export class MyReservationsComponent {
  private readonly reservations = inject(ReservationsService);
  private readonly settingsApi = inject(SettingsApi);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly settings = signal<PublicSettingsDto | null>(null);
  readonly showPast = signal(false);
  readonly cancelInFlight = signal<string | null>(null);

  readonly loading = this.reservations.loading;
  readonly error = this.reservations.error;

  readonly viewModels = computed<MyReservationVm[]>(() => {
    const now = Date.now();
    const minCancelMinutes = (this.settings()?.minCancellationHours ?? 2) * 60;
    return this.reservations.myReservations().map(r => toVm(r, now, minCancelMinutes));
  });

  readonly upcoming = computed(() =>
    this.viewModels()
      .filter(r => !r.isPast && r.status === ReservationStatus.Active)
      .sort((a, b) => a.startsAtDate.getTime() - b.startsAtDate.getTime()));

  readonly past = computed(() =>
    this.viewModels()
      .filter(r => r.isPast || r.status === ReservationStatus.Cancelled)
      .sort((a, b) => b.startsAtDate.getTime() - a.startsAtDate.getTime()));

  constructor() {
    void this.bootstrap();
  }

  togglePast(): void {
    this.showPast.update(v => !v);
  }

  async onCancel(r: MyReservationVm): Promise<void> {
    const confirmed = await firstValueFrom(this.dialog.open<
      ConfirmDialogComponent, ConfirmDialogData, boolean
    >(ConfirmDialogComponent, {
      data: {
        title: 'Buchung stornieren?',
        message: `Möchtest du die Buchung am ${format(r.startsAtDate, 'EEEE, d. MMMM', { locale: de })} um ${format(r.startsAtDate, 'HH:mm')} wirklich stornieren? Der Slot wird danach wieder freigegeben.`,
        confirmLabel: 'Stornieren',
        cancelLabel: 'Zurück',
        destructive: true
      },
      width: '440px'
    }).afterClosed());

    if (!confirmed) return;

    this.cancelInFlight.set(r.id);
    const result = await this.reservations.cancel(r.id, r.rowVersion);
    this.cancelInFlight.set(null);

    if (result.ok) {
      this.snackBar.open('Buchung storniert.', 'OK', { duration: 4000 });
      // Reload so cancelled entry shows up under "Vergangene" with the right
      // cancelledAt timestamp from the server.
      await this.reservations.loadMine();
    } else {
      this.snackBar.open(result.message, 'OK', { duration: 6000 });
    }
  }

  goBook(): void {
    this.router.navigate(['/reservations']);
  }

  formatDate(date: Date): string {
    return format(date, 'EEEE, d. MMMM yyyy', { locale: de });
  }

  formatTimeRange(start: Date, end: Date): string {
    return `${format(start, 'HH:mm')} – ${format(end, 'HH:mm')}`;
  }

  trackById(_: number, r: MyReservationVm): string {
    return r.id;
  }

  private async bootstrap(): Promise<void> {
    const [settings] = await Promise.all([
      firstValueFrom(this.settingsApi.getPublic()).catch(() => null),
      this.reservations.loadMine()
    ]);
    if (settings) this.settings.set(settings);
  }
}

function toVm(
  r: MyReservationDto,
  nowMs: number,
  minCancelMinutes: number
): MyReservationVm {
  const startsAtDate = new Date(r.startsAt);
  const endsAtDate = new Date(r.endsAt);
  const durationMinutes = differenceInMinutes(endsAtDate, startsAtDate);
  const isPast = startsAtDate.getTime() < nowMs;
  const minutesUntilStart = (startsAtDate.getTime() - nowMs) / 60_000;

  let canCancel = false;
  let cancelBlockedReason: string | null = null;

  if (r.status === ReservationStatus.Cancelled) {
    cancelBlockedReason = 'Bereits storniert';
  } else if (isPast) {
    cancelBlockedReason = 'Buchung liegt in der Vergangenheit';
  } else if (minutesUntilStart < minCancelMinutes) {
    const hours = Math.round(minCancelMinutes / 60);
    cancelBlockedReason = `Stornierung nur bis ${hours} Stunden vor Spielbeginn möglich`;
  } else {
    canCancel = true;
  }

  return {
    ...r,
    startsAtDate,
    endsAtDate,
    durationMinutes,
    isPast,
    canCancel,
    cancelBlockedReason
  };
}
