import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef
} from '@angular/material/dialog';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-stroked-button type="button" (click)="ref.close(false)">
        {{ data.cancelLabel ?? 'Abbrechen' }}
      </button>
      <button
        mat-flat-button
        type="button"
        class="confirm"
        [class.confirm--destructive]="data.destructive"
        (click)="ref.close(true)"
      >
        {{ data.confirmLabel ?? 'Bestätigen' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    :host { display: block; min-width: 360px; }
    h2 { margin: 0; padding: 1.5rem 1.5rem 0.5rem; font-size: 20px; font-weight: 600; }
    mat-dialog-content p { margin: 0; font-size: 16px; line-height: 1.6; }
    mat-dialog-actions { padding: 1rem 1.5rem 1.5rem; }
    .confirm { background: var(--tc-deep-navy); color: #fff; }
    .confirm--destructive { background: var(--tc-error); color: #fff; }
  `
})
export class ConfirmDialogComponent {
  readonly ref = inject(MatDialogRef<ConfirmDialogComponent, boolean>);
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
}
