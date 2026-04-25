import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { format } from 'date-fns';
import { de } from 'date-fns/locale';
import { firstValueFrom } from 'rxjs';

import { CourtBlocksApi } from '../../../core/api/court-blocks.api';
import { CourtsApi } from '../../../core/api/courts.api';
import { SeasonsApi } from '../../../core/api/seasons.api';
import { CourtBlockDto } from '../../../core/models/court-block.model';
import { CourtDto } from '../../../core/models/court.model';
import {
  ConfirmDialogComponent,
  ConfirmDialogData
} from '../../../shared/components/confirm-dialog.component';
import {
  CreateBlockDialogComponent,
  CreateBlockDialogData
} from './create-block-dialog.component';

interface BlockRow {
  block: CourtBlockDto;
  date: string;
  timeRange: string;
  isSeriesMember: boolean;
  seriesOccurrences: number;
}

@Component({
  selector: 'app-court-blocks-admin',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './court-blocks-admin.component.html',
  styleUrl: './court-blocks-admin.component.scss'
})
export class CourtBlocksAdminComponent {
  private readonly api = inject(CourtBlocksApi);
  private readonly courtsApi = inject(CourtsApi);
  private readonly seasonsApi = inject(SeasonsApi);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly blocks = signal<CourtBlockDto[]>([]);
  readonly courts = signal<CourtDto[]>([]);
  readonly seasonEndDate = signal<string>('');
  readonly loading = signal(true);

  readonly rows = computed<BlockRow[]>(() => {
    const all = this.blocks();
    const seriesCounts = new Map<string, number>();
    for (const b of all) {
      if (b.seriesId) seriesCounts.set(b.seriesId, (seriesCounts.get(b.seriesId) ?? 0) + 1);
    }
    return all.map(b => ({
      block: b,
      date: format(new Date(b.startsAt), 'EEE, d. MMM yyyy', { locale: de }),
      timeRange: `${format(new Date(b.startsAt), 'HH:mm')} – ${format(new Date(b.endsAt), 'HH:mm')}`,
      isSeriesMember: !!b.seriesId,
      seriesOccurrences: b.seriesId ? (seriesCounts.get(b.seriesId) ?? 1) : 1
    }));
  });

  constructor() {
    void this.bootstrap();
  }

  async openCreate(): Promise<void> {
    const ref = this.dialog.open<
      CreateBlockDialogComponent, CreateBlockDialogData, boolean
    >(CreateBlockDialogComponent, {
      data: {
        courts: this.courts().filter(c => c.isActive),
        defaultEndDate: this.seasonEndDate()
      },
      width: '560px',
      autoFocus: false
    });
    const created = await firstValueFrom(ref.afterClosed());
    if (created) {
      this.snackBar.open('Platzsperre angelegt.', 'OK', { duration: 3000 });
      void this.load();
    }
  }

  async deleteSingle(row: BlockRow): Promise<void> {
    const confirmed = await firstValueFrom(this.dialog.open<
      ConfirmDialogComponent, ConfirmDialogData, boolean
    >(ConfirmDialogComponent, {
      data: {
        title: 'Sperre entfernen?',
        message: `${row.block.courtName} am ${row.date} ${row.timeRange} wird wieder freigegeben.`,
        confirmLabel: 'Entfernen',
        cancelLabel: 'Abbrechen',
        destructive: true
      },
      width: '440px'
    }).afterClosed());
    if (!confirmed) return;

    try {
      await firstValueFrom(this.api.delete(row.block.id));
      this.snackBar.open('Sperre entfernt.', 'OK', { duration: 3000 });
      void this.load();
    } catch {
      this.snackBar.open('Entfernen fehlgeschlagen.', 'OK', { duration: 4000 });
    }
  }

  async deleteSeries(row: BlockRow): Promise<void> {
    if (!row.block.seriesId) return;

    const confirmed = await firstValueFrom(this.dialog.open<
      ConfirmDialogComponent, ConfirmDialogData, boolean
    >(ConfirmDialogComponent, {
      data: {
        title: 'Gesamte Serie entfernen?',
        message: `${row.seriesOccurrences} Termine der Serie „${row.block.reason}" auf ${row.block.courtName} werden entfernt.`,
        confirmLabel: 'Serie entfernen',
        cancelLabel: 'Abbrechen',
        destructive: true
      },
      width: '440px'
    }).afterClosed());
    if (!confirmed) return;

    try {
      await firstValueFrom(this.api.deleteSeries(row.block.seriesId));
      this.snackBar.open('Serie entfernt.', 'OK', { duration: 3000 });
      void this.load();
    } catch {
      this.snackBar.open('Serie konnte nicht entfernt werden.', 'OK', { duration: 4000 });
    }
  }

  private async bootstrap(): Promise<void> {
    const [courts, season] = await Promise.all([
      firstValueFrom(this.courtsApi.list(true)).catch(() => []),
      firstValueFrom(this.seasonsApi.current()).catch(() => null)
    ]);
    this.courts.set(courts);
    this.seasonEndDate.set(season?.endDate ?? '');
    await this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const list = await firstValueFrom(this.api.list());
      this.blocks.set(list);
    } finally {
      this.loading.set(false);
    }
  }
}
