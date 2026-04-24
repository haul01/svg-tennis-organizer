import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { firstValueFrom } from 'rxjs';

import { CourtsApi } from '../../../core/api/courts.api';
import { CourtDto } from '../../../core/models/court.model';
import { CourtDialogComponent, CourtDialogData } from './court-dialog.component';

@Component({
  selector: 'app-courts-admin',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './courts-admin.component.html',
  styleUrl: './courts-admin.component.scss'
})
export class CourtsAdminComponent {
  private readonly api = inject(CourtsApi);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly courts = signal<CourtDto[]>([]);
  readonly loading = signal(true);
  readonly toggling = signal<number | null>(null);

  constructor() {
    void this.load();
  }

  openCreate(): void {
    const ref = this.dialog.open<
      CourtDialogComponent, CourtDialogData, CourtDto | null
    >(CourtDialogComponent, {
      data: {},
      width: '460px',
      autoFocus: false
    });
    ref.afterClosed().subscribe(created => {
      if (created) {
        this.snackBar.open(`„${created.name}" angelegt.`, 'OK', { duration: 3000 });
        void this.load();
      }
    });
  }

  openEdit(court: CourtDto): void {
    const ref = this.dialog.open<
      CourtDialogComponent, CourtDialogData, CourtDto | null
    >(CourtDialogComponent, {
      data: { court },
      width: '460px',
      autoFocus: false
    });
    ref.afterClosed().subscribe(updated => {
      if (updated) {
        this.snackBar.open(`„${updated.name}" aktualisiert.`, 'OK', { duration: 3000 });
        void this.load();
      }
    });
  }

  async toggleActive(c: CourtDto): Promise<void> {
    this.toggling.set(c.id);
    try {
      await firstValueFrom(this.api.update(c.id, {
        name: c.name,
        displayOrder: c.displayOrder,
        isActive: !c.isActive
      }));
      this.snackBar.open(
        !c.isActive ? 'Platz aktiviert.' : 'Platz deaktiviert.',
        'OK',
        { duration: 3000 }
      );
      await this.load();
    } catch {
      this.snackBar.open('Statusänderung fehlgeschlagen.', 'OK', { duration: 4000 });
    } finally {
      this.toggling.set(null);
    }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const list = await firstValueFrom(this.api.list(true));
      this.courts.set(list);
    } finally {
      this.loading.set(false);
    }
  }
}
