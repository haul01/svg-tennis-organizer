import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { format, subDays } from 'date-fns';
import { de } from 'date-fns/locale';
import { firstValueFrom } from 'rxjs';

import { CourtsApi } from '../../../core/api/courts.api';
import { ReportsApi } from '../../../core/api/reports.api';
import { CourtDto } from '../../../core/models/court.model';
import { ReservationReportItemDto } from '../../../core/models/report.model';
import { ReservationStatus } from '../../reservations/reservation.model';

type StatusFilterValue = 'all' | 'active' | 'cancelled';

@Component({
  selector: 'app-reservations-report',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  templateUrl: './reservations-report.component.html',
  styleUrl: './reservations-report.component.scss'
})
export class ReservationsReportComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ReportsApi);
  private readonly courtsApi = inject(CourtsApi);

  readonly loading = signal(true);
  readonly courts = signal<CourtDto[]>([]);

  readonly items = signal<ReservationReportItemDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(25);

  // Default window: 30 days back through end of today.
  readonly form = this.fb.nonNullable.group({
    from: [subDays(startOfDay(new Date()), 30)],
    to: [startOfDay(new Date())],
    courtId: this.fb.control<number | null>(null),
    status: ['all' as StatusFilterValue]
  });

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  readonly rangeLabel = computed(() => {
    const total = this.totalCount();
    if (total === 0) return '0 Buchungen';
    const start = (this.page() - 1) * this.pageSize() + 1;
    const end = Math.min(this.page() * this.pageSize(), total);
    return `${start}–${end} von ${total} Buchungen`;
  });

  constructor() {
    void this.bootstrap();
  }

  applyFilter(): void {
    this.page.set(1);
    void this.load();
  }

  resetFilter(): void {
    this.form.reset({
      from: subDays(startOfDay(new Date()), 30),
      to: startOfDay(new Date()),
      courtId: null,
      status: 'all'
    });
    this.applyFilter();
  }

  goPrevPage(): void {
    if (this.page() > 1) {
      this.page.update(p => p - 1);
      void this.load();
    }
  }

  goNextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update(p => p + 1);
      void this.load();
    }
  }

  formatDate(iso: string): string {
    return format(new Date(iso), 'EE, d. MMM yyyy', { locale: de });
  }

  formatTimeRange(startIso: string, endIso: string): string {
    return `${format(new Date(startIso), 'HH:mm')}`
      + `–${format(new Date(endIso), 'HH:mm')}`;
  }

  formatCreated(iso: string): string {
    return format(new Date(iso), 'd. MMM yyyy, HH:mm', { locale: de });
  }

  statusLabel(s: ReservationStatus): string {
    return s === ReservationStatus.Cancelled ? 'Storniert' : 'Aktiv';
  }

  private async bootstrap(): Promise<void> {
    // Courts list powers the filter dropdown - include inactive ones so
    // historical bookings on retired courts can still be filtered.
    try {
      const courts = await firstValueFrom(this.courtsApi.list(true));
      this.courts.set(courts);
    } catch {
      this.courts.set([]);
    }
    await this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const raw = this.form.getRawValue();
      const status = raw.status === 'active'
        ? ReservationStatus.Active
        : raw.status === 'cancelled'
          ? ReservationStatus.Cancelled
          : undefined;
      const result = await firstValueFrom(this.api.listReservations({
        from: raw.from ?? undefined,
        // Server filter is StartsAt < to, so push the picked end-of-day
        // forward by one day to make `to` inclusive in the UI sense.
        to: raw.to ? new Date(raw.to.getTime() + 24 * 60 * 60_000) : undefined,
        courtId: raw.courtId ?? undefined,
        status,
        page: this.page(),
        pageSize: this.pageSize()
      }));
      this.items.set(result.items);
      this.totalCount.set(result.totalCount);
    } finally {
      this.loading.set(false);
    }
  }
}

function startOfDay(d: Date): Date {
  const out = new Date(d);
  out.setHours(0, 0, 0, 0);
  return out;
}
