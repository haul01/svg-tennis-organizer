import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { firstValueFrom } from 'rxjs';

import { MembersApi } from '../../../core/api/members.api';
import { MEMBER_ROLES, MemberDetailDto, MemberRole } from '../../../core/models/member.model';
import { ApiError, ValidationFailure } from '../../../core/models/result.model';

@Component({
  selector: 'app-create-member-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  templateUrl: './create-member-dialog.component.html',
  styleUrl: './create-member-dialog.component.scss'
})
export class CreateMemberDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MembersApi);
  private readonly ref = inject(MatDialogRef<CreateMemberDialogComponent, MemberDetailDto | null>);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly failures = signal<ValidationFailure[]>([]);
  readonly availableRoles = MEMBER_ROLES;

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    role: ['Member' as MemberRole, Validators.required]
  });

  cancel(): void { this.ref.close(null); }

  async submit(): Promise<void> {
    if (this.submitting()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.failures.set([]);

    try {
      const created = await firstValueFrom(this.api.create(this.form.getRawValue()));
      this.ref.close(created);
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 400) {
        const body = err.error as ApiError | undefined;
        this.errorMessage.set(body?.error ?? 'Mitglied konnte nicht angelegt werden.');
        this.failures.set(body?.failures ?? []);
      } else {
        this.errorMessage.set('Mitglied konnte nicht angelegt werden.');
      }
    } finally {
      this.submitting.set(false);
    }
  }
}
