import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { firstValueFrom } from 'rxjs';

import { CourtsApi } from '../../../core/api/courts.api';
import { CourtDto } from '../../../core/models/court.model';
import { ApiError, ValidationFailure } from '../../../core/models/result.model';

export interface CourtDialogData {
  court?: CourtDto;
}

@Component({
  selector: 'app-court-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule
  ],
  templateUrl: './court-dialog.component.html',
  styleUrl: './court-dialog.component.scss'
})
export class CourtDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CourtsApi);
  private readonly ref = inject(MatDialogRef<CourtDialogComponent, CourtDto | null>);
  readonly data = inject<CourtDialogData>(MAT_DIALOG_DATA);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly failures = signal<ValidationFailure[]>([]);

  readonly isEdit = !!this.data.court;

  readonly form = this.fb.nonNullable.group({
    name: [this.data.court?.name ?? '', [Validators.required, Validators.maxLength(50)]],
    displayOrder: this.fb.control<number | null>(this.data.court?.displayOrder ?? null),
    isActive: [this.data.court?.isActive ?? true],
    isGuestBookable: [this.data.court?.isGuestBookable ?? false]
  });

  cancel(): void { this.ref.close(null); }

  async submit(): Promise<void> {
    if (this.submitting()) return;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.failures.set([]);

    try {
      const raw = this.form.getRawValue();
      const court = this.data.court;
      const saved = court
        ? await firstValueFrom(this.api.update(court.id, {
            name: raw.name,
            displayOrder: raw.displayOrder ?? court.displayOrder,
            isActive: raw.isActive,
            isGuestBookable: raw.isGuestBookable
          }))
        : await firstValueFrom(this.api.create({
            name: raw.name,
            displayOrder: raw.displayOrder,
            isGuestBookable: raw.isGuestBookable
          }));
      this.ref.close(saved);
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 400) {
        const body = err.error as ApiError | undefined;
        this.errorMessage.set(body?.error ?? 'Speichern fehlgeschlagen.');
        this.failures.set(body?.failures ?? []);
      } else {
        this.errorMessage.set('Speichern fehlgeschlagen.');
      }
    } finally {
      this.submitting.set(false);
    }
  }
}
