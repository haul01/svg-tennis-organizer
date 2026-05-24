import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface ApplyMembershipRequest {
  firstName: string;
  lastName: string;
  street: string;
  postalCode: string;
  city: string;
  birthDate: string;       // ISO date YYYY-MM-DD
  phone: string;
  email: string;
  feeTier: MembershipFeeTier;
  comment?: string | null;
}

export type MembershipFeeTier =
  | 'adult'
  | 'youth'
  | 'child'
  | 'couple'
  | 'adult-child';

export interface MembershipFeeOption {
  value: MembershipFeeTier;
  label: string;
  description?: string;
}

export const MEMBERSHIP_FEE_OPTIONS: ReadonlyArray<MembershipFeeOption> = [
  { value: 'adult',       label: 'Erwachsene',                  description: '€ 100,-' },
  { value: 'youth',       label: 'Jugendliche bis 18 Jahre',     description: '€ 30,-' },
  { value: 'child',       label: 'Kinder / Schüler bis 14 Jahre',description: '€ 15,-' },
  { value: 'couple',      label: 'Kombi Ehepaare',               description: '€ 190,-' },
  { value: 'adult-child', label: 'Kombi Erwachsener + Kind',     description: '€ 100,-' }
];

@Injectable({ providedIn: 'root' })
export class MembershipApplyApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/membership/apply`;

  apply(req: ApplyMembershipRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(this.url, req);
  }
}
