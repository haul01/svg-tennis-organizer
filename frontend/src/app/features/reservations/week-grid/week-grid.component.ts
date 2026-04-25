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
import { addDays, endOfDay, format, startOfDay, startOfWeek } from 'date-fns';
import { de } from 'date-fns/locale';
import { firstValueFrom } from 'rxjs';

import { CourtBlocksApi } from '../../../core/api/court-blocks.api';
import { CourtsApi } from '../../../core/api/courts.api';
import { SeasonsApi } from '../../../core/api/seasons.api';
import { CourtBlockDto } from '../../../core/models/court-block.model';
import { CourtDto } from '../../../core/models/court.model';
import { SeasonDto } from '../../../core/models/season.model';
import {
  BookingDialogComponent,
  BookingDialogData,
  BookingDialogResult
} from '../booking-dialog/booking-dialog.component';
import { WeekReservationDto } from '../reservation.model';
import { ReservationsService } from '../reservations.service';

type CellState = 'free' | 'mine' | 'busy' | 'blocked' | 'past';

interface Cell {
  state: CellState;
  startsAt: Date;
  endsAt: Date;
  courtId: number;
  reservation?: WeekReservationDto;
  blockReason?: string;
}

@Component({
  selector: 'app-week-grid',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './week-grid.component.html',
  styleUrl: './week-grid.component.scss'
})
export class WeekGridComponent {
  private readonly reservations = inject(ReservationsService);
  private readonly courtsApi = inject(CourtsApi);
  private readonly seasonsApi = inject(SeasonsApi);
  private readonly blocksApi = inject(CourtBlocksApi);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  // State
  readonly courts = signal<CourtDto[]>([]);
  readonly season = signal<SeasonDto | null>(null);
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

  readonly grid = computed<Cell[][]>(() => {
    const season = this.season();
    const courts = this.courts();
    if (!season || courts.length === 0) return [];

    const day = this.selectedDate();
    const now = new Date();
    const labels = this.timeSlots();
    const slotMinutes = season.slotDurationMinutes;

    // Index reservations by (courtId + slot ISO start) for O(1) lookup.
    const byKey = new Map<string, WeekReservationDto>();
    for (const r of this.weekReservations()) {
      const starts = new Date(r.startsAt);
      byKey.set(keyOf(r.courtId, starts), r);
    }

    // Convert blocks to Date-intervals once; filter by selected day
    // to keep the per-cell intersection cheap.
    const dayBlocks = this.blocks()
      .filter(b => b.courtId !== undefined)
      .map(b => ({
        courtId: b.courtId,
        reason: b.reason,
        startsAt: new Date(b.startsAt),
        endsAt: new Date(b.endsAt)
      }))
      .filter(b => b.endsAt > startOfDay(day) && b.startsAt < endOfDay(day));

    return labels.map(label => {
      const [h, m] = label.split(':').map(Number);
      const rowStart = new Date(day);
      rowStart.setHours(h, m, 0, 0);
      const rowEnd = new Date(rowStart.getTime() + slotMinutes * 60_000);

      return courts.map<Cell>(court => {
        const reservation = byKey.get(keyOf(court.id, rowStart));
        const block = dayBlocks.find(b =>
          b.courtId === court.id && b.startsAt < rowEnd && b.endsAt > rowStart);

        let state: CellState;
        let blockReason: string | undefined;
        if (reservation) {
          state = reservation.isMine ? 'mine' : 'busy';
        } else if (block) {
          state = 'blocked';
          blockReason = block.reason;
        } else if (rowEnd <= now) {
          state = 'past';
        } else {
          state = 'free';
        }

        return { state, startsAt: rowStart, endsAt: rowEnd, courtId: court.id, reservation, blockReason };
      });
    });
  });

  constructor() {
    // One-shot bootstrap of courts + season.
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

  onCellClick(cell: Cell): void {
    if (cell.state !== 'free') return;

    const court = this.courts().find(c => c.id === cell.courtId);
    const data: BookingDialogData = {
      courtId: cell.courtId,
      courtName: court?.name ?? `Platz ${cell.courtId}`,
      startsAt: cell.startsAt,
      endsAt: cell.endsAt
    };

    const ref = this.dialog.open<
      BookingDialogComponent,
      BookingDialogData,
      BookingDialogResult
    >(BookingDialogComponent, { data, width: '480px', autoFocus: false });

    ref.afterClosed().subscribe(result => {
      if (result?.ok) {
        this.snackBar.open('Buchung bestätigt. Bestätigungsmail unterwegs.', 'OK', {
          duration: 4000
        });
      }
    });
  }

  private async bootstrap(): Promise<void> {
    try {
      const [courts, season] = await Promise.all([
        firstValueFrom(this.courtsApi.list()),
        firstValueFrom(this.seasonsApi.current())
      ]);
      this.courts.set(courts);
      this.season.set(season);
    } finally {
      this.bootstrapping.set(false);
    }
  }
}

function todayIndex(): number {
  const dayIdx = new Date().getDay(); // Sun=0, Mon=1, ...
  return (dayIdx + 6) % 7;             // Shift so Monday=0, Sunday=6
}

function keyOf(courtId: number, startsAt: Date): string {
  return `${courtId}|${startsAt.toISOString()}`;
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
