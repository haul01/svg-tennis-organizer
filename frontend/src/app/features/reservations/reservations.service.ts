import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ApiError, ValidationFailure } from '../../core/models/result.model';
import {
  CreateReservationRequest,
  ListMineOptions,
  MyReservationDto,
  WeekReservationDto
} from './reservation.model';
import { ReservationsApi } from './reservations.api';

export type CreateResult =
  | { ok: true; id: string }
  | { ok: false; status: 'invalid'; message?: string; failures?: ValidationFailure[] }
  | { ok: false; status: 'conflict'; message: string }
  | { ok: false; status: 'error'; message: string };

export type CancelResult =
  | { ok: true }
  | { ok: false; status: 'notfound' | 'invalid' | 'conflict' | 'error'; message: string };

/**
 * Owns the in-memory state around reservations. State is exposed as
 * readonly signals; mutations only go through explicit service methods.
 */
@Injectable({ providedIn: 'root' })
export class ReservationsService {
  private readonly api = inject(ReservationsApi);

  private readonly _weekReservations = signal<WeekReservationDto[]>([]);
  private readonly _myReservations = signal<MyReservationDto[]>([]);
  private readonly _weekStart = signal<Date | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly weekReservations = this._weekReservations.asReadonly();
  readonly myReservations = this._myReservations.asReadonly();
  readonly weekStart = this._weekStart.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  async loadWeek(weekStart: Date): Promise<void> {
    this._loading.set(true);
    this._error.set(null);
    try {
      const data = await firstValueFrom(this.api.getWeek(weekStart));
      this._weekReservations.set(data);
      this._weekStart.set(weekStart);
    } catch (err) {
      this._error.set(messageFrom(err, 'Laden der Woche fehlgeschlagen.'));
      this._weekReservations.set([]);
    } finally {
      this._loading.set(false);
    }
  }

  async loadMine(opts: ListMineOptions = {}): Promise<void> {
    this._loading.set(true);
    this._error.set(null);
    try {
      const data = await firstValueFrom(this.api.getMine(opts));
      this._myReservations.set(data);
    } catch (err) {
      this._error.set(messageFrom(err, 'Laden der Buchungen fehlgeschlagen.'));
      this._myReservations.set([]);
    } finally {
      this._loading.set(false);
    }
  }

  async create(req: CreateReservationRequest): Promise<CreateResult> {
    try {
      const response = await firstValueFrom(this.api.create(req));
      // Refresh the visible week so the new slot shows up immediately.
      const week = this._weekStart();
      if (week) await this.loadWeek(week);
      return { ok: true, id: response.id };
    } catch (err) {
      if (err instanceof HttpErrorResponse) {
        if (err.status === 400) {
          const body = err.error as ApiError | undefined;
          return { ok: false, status: 'invalid', message: body?.error, failures: body?.failures };
        }
        if (err.status === 409) {
          return {
            ok: false,
            status: 'conflict',
            message: (err.error as ApiError | undefined)?.error
              ?? 'Der Slot wurde gerade von jemand anderem gebucht.'
          };
        }
      }
      return { ok: false, status: 'error', message: messageFrom(err, 'Buchung fehlgeschlagen.') };
    }
  }

  async cancel(id: string, rowVersion: string): Promise<CancelResult> {
    try {
      await firstValueFrom(this.api.cancel(id, rowVersion));
      // Keep local lists in sync without a full roundtrip.
      this._myReservations.update(list =>
        list.map(r => r.id === id
          ? { ...r, status: 1 /* Cancelled */, cancelledAt: new Date().toISOString() }
          : r));
      this._weekReservations.update(list => list.filter(r => r.id !== id));
      return { ok: true };
    } catch (err) {
      if (err instanceof HttpErrorResponse) {
        if (err.status === 404) {
          return { ok: false, status: 'notfound', message: 'Buchung nicht gefunden.' };
        }
        if (err.status === 400) {
          return {
            ok: false,
            status: 'invalid',
            message: (err.error as ApiError | undefined)?.error ?? 'Stornierung nicht möglich.'
          };
        }
        if (err.status === 409) {
          return {
            ok: false,
            status: 'conflict',
            message: (err.error as ApiError | undefined)?.error
              ?? 'Die Buchung wurde inzwischen geändert, bitte neu laden.'
          };
        }
      }
      return { ok: false, status: 'error', message: messageFrom(err, 'Stornierung fehlgeschlagen.') };
    }
  }

  clearError(): void {
    this._error.set(null);
  }
}

function messageFrom(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as ApiError | undefined;
    if (body?.error) return body.error;
    if (err.status === 0) return 'Server ist nicht erreichbar.';
  }
  return fallback;
}
