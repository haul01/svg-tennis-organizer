import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';

import { ImportCsvSummary, MembersApi } from '../../../core/api/members.api';

@Component({
  selector: 'app-import-csv-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './import-csv-dialog.component.html',
  styleUrl: './import-csv-dialog.component.scss'
})
export class ImportCsvDialogComponent {
  private readonly api = inject(MembersApi);
  readonly ref = inject(
    MatDialogRef<ImportCsvDialogComponent, ImportCsvSummary | null>
  );

  readonly file = signal<File | null>(null);
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly summary = signal<ImportCsvSummary | null>(null);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selected = input.files?.[0] ?? null;
    this.file.set(selected);
    this.errorMessage.set(null);
    this.summary.set(null);
  }

  close(): void {
    this.ref.close(this.summary());
  }

  async submit(): Promise<void> {
    const f = this.file();
    if (!f || this.submitting()) return;

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.summary.set(null);

    try {
      const result = await firstValueFrom(this.api.importCsv(f));
      this.summary.set(result);
    } catch (err) {
      if (err instanceof HttpErrorResponse) {
        const body = err.error as { error?: string } | undefined;
        this.errorMessage.set(body?.error ?? 'Import fehlgeschlagen.');
      } else {
        this.errorMessage.set('Import fehlgeschlagen.');
      }
    } finally {
      this.submitting.set(false);
    }
  }
}
