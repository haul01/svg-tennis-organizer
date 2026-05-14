import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DatePipe } from '@angular/common';
import { addDays, endOfDay, format, startOfDay, startOfWeek } from 'date-fns';
import { de } from 'date-fns/locale';
import { firstValueFrom } from 'rxjs';

import {
  ConfirmDialogComponent,
  ConfirmDialogData
} from '../../../shared/components/confirm-dialog.component';

import { CourtBlocksApi } from '../../../core/api/court-blocks.api';
import { CourtsApi } from '../../../core/api/courts.api';
import { SeasonsApi } from '../../../core/api/seasons.api';
import { SettingsApi } from '../../../core/api/settings.api';
import { CourtBlockDto } from '../../../core/models/court-block.model';
import { CourtDto } from '../../../core/models/court.model';
import { SeasonDto } from '../../../core/models/season.model';
import { PublicSettingsDto } from '../../../core/models/settings.model';
import {
  BookingDialogComponent,
  BookingDialogData,
  BookingDialogResult
} from '../booking-dialog/booking-dialog.component';
import { WeekReservationDto } from '../reservation.model';
import { ReservationsService } from '../reservations.service';

// Two-layer grid model.
//
// Layer 1 (slot grid): one SlotCell per (slot row x court). Knows only
// time-based state - is the slot in the past or open for booking?
// Reservations and blocks are NOT considered here.
//
// Layer 2 (booking tiles): one BookingTile per reservation/block,
// placed in the same CSS-grid container via grid-row span. Tiles sit
// on top of the slot layer (z-index) and visually cover the slots
// underneath, so multi-slot bookings appear as one continuous block.

type SlotState = 'free' | 'past';

interface SlotCell {
  state: SlotState;
  startsAt: Date;
  endsAt: Date;
  courtId: number;
}

type TileState = 'mine' | 'busy' | 'blocked';

interface BookingTile {
  state: TileState;
  /** 0-based slot index where the tile starts. */
  rowStart: number;
  /** Number of slot rows the tile covers. */
  rowSpan: number;
  /** 0-based court column index. */
  courtCol: number;
  courtId: number;
  /** Real booking/block start, used for the label time range. */
  startsAt: Date;
  /** Real booking/block end. */
  endsAt: Date;
  reservation?: WeekReservationDto;
  blockReason?: string;
}

@Component({
  selector: 'app-week-grid',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './week-grid.component.html',
  styleUrl: './week-grid.component.scss'
})
export class WeekGridComponent {
  private readonly reservations = inject(ReservationsService);
  private readonly courtsApi = inject(CourtsApi);
  private readonly seasonsApi = inject(SeasonsApi);
  private readonly blocksApi = inject(CourtBlocksApi);
  private readonly settingsApi = inject(SettingsApi);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  // State
  readonly courts = signal<CourtDto[]>([]);
  readonly season = signal<SeasonDto | null>(null);
  readonly settings = signal<PublicSettingsDto | null>(null);
  readonly blocks = signal<CourtBlockDto[]>([]);
  readonly bootstrapping = signal(true);
  readonly weekStart = signal<Date>(startOfWeek(new Date(), { weekStartsOn: 1 }));
  readonly selectedDayIndex = signal<number>(todayIndex());

  // Expose service signals to the template
  readonly loading = this.reservations.loading;
  readonly error = this.reservations.error;
  readonly weekReservations = this.reservations.weekReservations;

  // Derived
  readonly weekDays = computed(() =>
    Array.from({ length: 7 }, (_, i) => addDays(this.weekStart(), i)));

  readonly selectedDate = computed(() => this.weekDays()[this.selectedDayIndex()]);

  readonly selectedDateLabel = computed(() =>
    format(this.selectedDate(), "EEEE, d. MMMM yyyy", { locale: de }));

  readonly timeSlots = computed<string[]>(() => {
    const s = this.season();
    if (!s) return [];
    return buildSlotLabels(s.openingTime, s.closingTime, s.slotDurationMinutes);
  });

  /** Slot-state grid: free or past, per (row x court). Booking-agnostic. */
  readonly grid = computed<SlotCell[][]>(() => {
    const season = this.season();
    const courts = this.courts();
    if (!season || courts.length === 0) return [];

    const day = this.selectedDate();
    const now = new Date();
    const labels = this.timeSlots();
    const slotMinutes = season.slotDurationMinutes;

    return labels.map(label => {
      const [h, m] = label.split(':').map(Number);
      const rowStart = new Date(day);
      rowStart.setHours(h, m, 0, 0);
      const rowEnd = new Date(rowStart.getTime() + slotMinutes * 60_000);
      const state: SlotState = rowEnd <= now ? 'past' : 'free';

      return courts.map<SlotCell>(court => ({
        state,
        startsAt: rowStart,
        endsAt: rowEnd,
        courtId: court.id
      }));
    });
  });

  /** Overlay tiles for reservations + blocks. Flat list, one per booking. */
  readonly tiles = computed<BookingTile[]>(() => {
    const season = this.season();
    const courts = this.courts();
    if (!season || courts.length === 0) return [];

    const day = this.selectedDate();
    const labels = this.timeSlots();
    const slotMinutes = season.slotDurationMinutes;

    const dayStart = startOfDay(day);
    const dayEnd = endOfDay(day);

    // Lookup: courtId -> column index.
    const courtColById = new Map<number, number>();
    courts.forEach((c, i) => courtColById.set(c.id, i));

    // Slot-row start times for placing tiles on the grid.
    const rowStarts = labels.map(label => {
      const [h, m] = label.split(':').map(Number);
      const d = new Date(day);
      d.setHours(h, m, 0, 0);
      return d;
    });

    const result: BookingTile[] = [];

    const place = (
      courtId: number,
      starts: Date,
      ends: Date,
      state: TileState,
      extras: { reservation?: WeekReservationDto; blockReason?: string }
    ): void => {
      const courtCol = courtColById.get(courtId);
      if (courtCol === undefined) return;
      if (ends <= dayStart || starts >= dayEnd) return;

      // First slot row that the booking enters - any slot whose end is
      // after `starts` works. Clamps to row 0 when the booking begins
      // before the visible day.
      let rowStart = -1;
      for (let i = 0; i < rowStarts.length; i++) {
        const slotEnd = new Date(rowStarts[i].getTime() + slotMinutes * 60_000);
        if (starts < slotEnd) {
          rowStart = i;
          break;
        }
      }
      if (rowStart < 0) return;

      // Count how many subsequent slot rows the booking still covers.
      let rowSpan = 1;
      for (let i = rowStart + 1; i < rowStarts.length; i++) {
        if (rowStarts[i] < ends) rowSpan++;
        else break;
      }

      result.push({
        state,
        rowStart,
        rowSpan,
        courtCol,
        courtId,
        startsAt: starts,
        endsAt: ends,
        ...extras
      });
    };

    for (const r of this.weekReservations()) {
      place(
        r.courtId,
        new Date(r.startsAt),
        new Date(r.endsAt),
        r.isMine ? 'mine' : 'busy',
        { reservation: r }
      );
    }

    for (const b of this.blocks()) {
      if (b.courtId === undefined) continue;
      place(
        b.courtId,
        new Date(b.startsAt),
        new Date(b.endsAt),
        'blocked',
        { blockReason: b.reason }
      );
    }

    return result;
  });

  constructor() {
    // One-shot bootstrap of courts + season + settings.
    void this.bootstrap();

    // Re-load the week whenever the selected week changes.
    effect(() => {
      const ws = this.weekStart();
      void this.reservations.loadWeek(ws);
      void this.loadBlocksForWeek(ws);
    });
  }

  private async loadBlocksForWeek(weekStart: Date): Promise<void> {
    try {
      const blocks = await firstValueFrom(this.blocksApi.forWeek(weekStart));
      this.blocks.set(blocks);
    } catch {
      // Grid still works without blocks; admin screen surfaces errors.
      this.blocks.set([]);
    }
  }

  dayLabel(date: Date): string {
    return format(date, 'EEE, d. MMM', { locale: de });
  }

  selectDay(index: number): void {
    this.selectedDayIndex.set(index);
  }

  goToday(): void {
    this.weekStart.set(startOfWeek(new Date(), { weekStartsOn: 1 }));
    this.selectedDayIndex.set(todayIndex());
  }

  goPreviousWeek(): void {
    this.weekStart.update(d => addDays(d, -7));
  }

  goNextWeek(): void {
    this.weekStart.update(d => addDays(d, 7));
  }

  onSlotClick(cell: SlotCell): void {
    if (cell.state !== 'free') return;

    const court = this.courts().find(c => c.id === cell.courtId);
    const season = this.season();
    if (!season) return;

    const data: BookingDialogData = {
      courtId: cell.courtId,
      courtName: court?.name ?? `Platz ${cell.courtId}`,
      startsAt: cell.startsAt,
      slotMinutes: season.slotDurationMinutes,
      maxSlots: this.settings()?.maxSlotsPerBooking ?? 4,
      guestMembershipPromptText: this.settings()?.guestMembershipPromptText ?? ''
    };

    const ref = this.dialog.open<
      BookingDialogComponent,
      BookingDialogData,
      BookingDialogResult
    >(BookingDialogComponent, {
      data,
      width: '480px',
      maxWidth: '95vw', // prevent overflow on phones < 480px wide
      autoFocus: false
    });

    ref.afterClosed().subscribe(result => {
      if (result?.ok) {
        this.snackBar.open('Buchung bestätigt. Bestätigungsmail unterwegs.', 'OK', {
          duration: 4000
        });
      }
    });
  }

  async onTileClick(tile: BookingTile): Promise<void> {
    // Only my own bookings are cancel-able from the grid. Foreign +
    // blocked tiles have nothing to do on click.
    if (tile.state !== 'mine' || !tile.reservation) return;

    const dateLabel = format(tile.startsAt, 'EEEE, d. MMMM', { locale: de });
    const timeRange = `${format(tile.startsAt, 'HH:mm')}–${format(tile.endsAt, 'HH:mm')}`;

    const confirmed = await firstValueFrom(this.dialog.open<
      ConfirmDialogComponent, ConfirmDialogData, boolean
    >(ConfirmDialogComponent, {
      data: {
        title: 'Buchung stornieren?',
        message: `Möchtest du deine Buchung am ${dateLabel} um ${timeRange} wirklich stornieren? Der Slot wird danach wieder freigegeben.`,
        confirmLabel: 'Stornieren',
        cancelLabel: 'Zurück',
        destructive: true
      },
      width: '440px',
      maxWidth: '95vw'
    }).afterClosed());

    if (!confirmed) return;

    const result = await this.reservations.cancel(tile.reservation.id);
    if (result.ok) {
      // ReservationsService.cancel already drops the reservation from
      // weekReservations(), so tiles() recomputes and the tile vanishes.
      this.snackBar.open('Buchung storniert.', 'OK', { duration: 4000 });
    } else {
      this.snackBar.open(result.message, 'OK', { duration: 6000 });
    }
  }

  private async bootstrap(): Promise<void> {
    try {
      const [courts, season, settings] = await Promise.all([
        firstValueFrom(this.courtsApi.list()),
        firstValueFrom(this.seasonsApi.current()),
        firstValueFrom(this.settingsApi.getPublic())
      ]);
      this.courts.set(courts);
      this.season.set(season);
      this.settings.set(settings);
    } finally {
      this.bootstrapping.set(false);
    }
  }
}

function todayIndex(): number {
  const dayIdx = new Date().getDay(); // Sun=0, Mon=1, ...
  return (dayIdx + 6) % 7;             // Shift so Monday=0, Sunday=6
}

function buildSlotLabels(openingTime: string, closingTime: string, slotMinutes: number): string[] {
  const [oh, om] = openingTime.split(':').map(Number);
  const [ch, cm] = closingTime.split(':').map(Number);
  const openMinutes = oh * 60 + om;
  const closeMinutes = ch * 60 + cm;
  const slots: string[] = [];
  for (let t = openMinutes; t + slotMinutes <= closeMinutes; t += slotMinutes) {
    const h = Math.floor(t / 60).toString().padStart(2, '0');
    const m = (t % 60).toString().padStart(2, '0');
    slots.push(`${h}:${m}`);
  }
  return slots;
}
