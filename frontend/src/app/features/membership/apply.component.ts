import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';

import {
  ApplyMembershipRequest,
  MembershipApplyApi,
  MembershipFeeTier,
  MEMBERSHIP_FEE_OPTIONS
} from './membership-apply.api';

// Tiers that cover more than one person. Selecting them must surface a
// hint that each additional person needs their own application form.
const MULTI_PERSON_TIERS: ReadonlySet<MembershipFeeTier> = new Set([
  'couple', 'adult-child'
]);

@Component({
  selector: 'app-membership-apply',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './apply.component.html',
  styleUrl: './apply.component.scss'
})
export class MembershipApplyComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MembershipApplyApi);

  readonly feeOptions = MEMBERSHIP_FEE_OPTIONS;
  readonly currentYear = new Date().getFullYear();

  readonly submitting = signal(false);
  readonly submitted = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    street: ['', [Validators.required, Validators.maxLength(200)]],
    postalCode: ['', [Validators.required, Validators.maxLength(20)]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    birthDate: this.fb.nonNullable.control<Date | null>(null, Validators.required),
    phone: ['', [Validators.required, Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    feeTier: this.fb.nonNullable.control<MembershipFeeTier | null>(null, Validators.required),
    comment: ['', Validators.maxLength(2000)],
    acceptStatutes: [false, Validators.requiredTrue]
  });

  // toSignal lets a zoneless OnPush component re-render when the form
  // state changes, so the radio-card "checked" highlight and the
  // conditional kombi-hint stay in sync without manual markForCheck.
  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue()
  });
  readonly selectedFeeTier = computed(() => this.formValue().feeTier ?? null);
  readonly isMultiPersonTier = computed(() => {
    const tier = this.selectedFeeTier();
    return tier !== null && MULTI_PERSON_TIERS.has(tier);
  });

  selectFeeTier(tier: MembershipFeeTier): void {
    this.form.controls.feeTier.setValue(tier);
    this.form.controls.feeTier.markAsTouched();
  }

  async submit(): Promise<void> {
    if (this.submitting()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const birth = raw.birthDate;
    if (!birth || !raw.feeTier) return;

    const isoBirth = formatLocalDateIso(birth);
    const payload: ApplyMembershipRequest = {
      firstName: raw.firstName.trim(),
      lastName: raw.lastName.trim(),
      street: raw.street.trim(),
      postalCode: raw.postalCode.trim(),
      city: raw.city.trim(),
      birthDate: isoBirth,
      phone: raw.phone.trim(),
      email: raw.email.trim(),
      feeTier: raw.feeTier,
      comment: raw.comment.trim() || null
    };

    this.submitting.set(true);
    this.errorMessage.set(null);

    try {
      await firstValueFrom(this.api.apply(payload));
      this.submitted.set(true);
    } catch (err) {
      if (err instanceof HttpErrorResponse) {
        if (err.status === 429) {
          this.errorMessage.set(
            'Es wurden zu viele Anträge gesendet. Bitte versuche es in einer Stunde erneut.'
          );
        } else {
          const body = err.error as { error?: string } | undefined;
          this.errorMessage.set(
            body?.error
              ?? 'Der Antrag konnte nicht übermittelt werden. Bitte später erneut versuchen.'
          );
        }
      } else {
        this.errorMessage.set('Der Antrag konnte nicht übermittelt werden.');
      }
    } finally {
      this.submitting.set(false);
    }
  }
}

// Native Date -> ISO YYYY-MM-DD without timezone shift. The Material
// datepicker hands back a Date at local midnight, so toISOString() can
// roll backwards in time zones west of UTC.
function formatLocalDateIso(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}
