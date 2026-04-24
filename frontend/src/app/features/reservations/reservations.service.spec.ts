import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import {
  CreateReservationRequest,
  CreateReservationResponse,
  ListMineOptions,
  MyReservationDto,
  ReservationStatus,
  WeekReservationDto
} from './reservation.model';
import { ReservationsApi } from './reservations.api';
import { ReservationsService } from './reservations.service';

class FakeApi {
  getWeekCalls: Date[] = [];
  getMineCalls: ListMineOptions[] = [];
  createCalls: CreateReservationRequest[] = [];
  cancelCalls: { id: string; rowVersion: string }[] = [];

  weekResponse: Observable<WeekReservationDto[]> = of([]);
  mineResponse: Observable<MyReservationDto[]> = of([]);
  createResponse: Observable<CreateReservationResponse> = of({ id: 'new-id' });
  cancelResponse: Observable<void> = of(void 0);

  getWeek(weekStart: Date): Observable<WeekReservationDto[]> {
    this.getWeekCalls.push(weekStart);
    return this.weekResponse;
  }
  getMine(opts: ListMineOptions = {}): Observable<MyReservationDto[]> {
    this.getMineCalls.push(opts);
    return this.mineResponse;
  }
  create(req: CreateReservationRequest): Observable<CreateReservationResponse> {
    this.createCalls.push(req);
    return this.createResponse;
  }
  cancel(id: string, rowVersion: string): Observable<void> {
    this.cancelCalls.push({ id, rowVersion });
    return this.cancelResponse;
  }
}

function httpError(status: number, body?: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, statusText: 'test', error: body });
}

const sampleWeek: WeekReservationDto[] = [
  {
    id: 'r1',
    courtId: 1,
    startsAt: '2026-06-10T18:00:00+02:00',
    endsAt: '2026-06-10T19:00:00+02:00',
    isMine: true,
    guestName: null
  }
];

function setup(): { service: ReservationsService; api: FakeApi } {
  const api = new FakeApi();
  TestBed.configureTestingModule({
    providers: [{ provide: ReservationsApi, useValue: api }]
  });
  return { service: TestBed.inject(ReservationsService), api };
}

describe('ReservationsService', () => {
  const weekStart = new Date('2026-06-08T00:00:00.000Z');
  const sampleReq: CreateReservationRequest = {
    courtId: 1,
    startsAt: '2026-06-10T18:00:00+02:00',
    endsAt: '2026-06-10T19:00:00+02:00',
    guestPlayerId: null
  };

  it('loadWeek stores the data and clears loading + error', async () => {
    const { service, api } = setup();
    api.weekResponse = of(sampleWeek);

    await service.loadWeek(weekStart);

    expect(service.weekReservations()).toEqual(sampleWeek);
    expect(service.loading()).toBe(false);
    expect(service.error()).toBeNull();
    expect(service.weekStart()).toEqual(weekStart);
  });

  it('loadWeek surfaces server errors via the error signal', async () => {
    const { service, api } = setup();
    api.weekResponse = throwError(() => httpError(500, { error: 'boom' }));

    await service.loadWeek(weekStart);

    expect(service.error()).toBe('boom');
    expect(service.weekReservations()).toEqual([]);
    expect(service.loading()).toBe(false);
  });

  it('create reloads the current week on success', async () => {
    const { service, api } = setup();
    api.weekResponse = of(sampleWeek);
    await service.loadWeek(weekStart);
    expect(api.getWeekCalls).toHaveLength(1);

    const result = await service.create(sampleReq);

    expect(result).toEqual({ ok: true, id: 'new-id' });
    expect(api.getWeekCalls).toHaveLength(2);
  });

  it('create maps 409 to a conflict result without touching state', async () => {
    const { service, api } = setup();
    api.createResponse = throwError(() =>
      httpError(409, { error: 'Der Slot wurde gerade von jemand anderem gebucht.' }));

    const result = await service.create(sampleReq);

    expect(result).toEqual({
      ok: false,
      status: 'conflict',
      message: 'Der Slot wurde gerade von jemand anderem gebucht.'
    });
  });

  it('create maps 400 with failures to an invalid result', async () => {
    const { service, api } = setup();
    const failures = [{ code: 'IN_PAST', message: 'Der Slot liegt in der Vergangenheit.' }];
    api.createResponse = throwError(() => httpError(400, { error: null, failures }));

    const result = await service.create(sampleReq);

    expect(result.ok).toBe(false);
    if (!result.ok && result.status === 'invalid') {
      expect(result.failures).toEqual(failures);
    } else {
      throw new Error(`expected invalid result, got ${JSON.stringify(result)}`);
    }
  });

  it('cancel flips myReservations status and prunes from the week view', async () => {
    const { service, api } = setup();
    api.mineResponse = of([
      {
        id: 'r1',
        courtId: 1,
        courtName: 'Platz 1',
        startsAt: '2026-06-10T18:00:00+02:00',
        endsAt: '2026-06-10T19:00:00+02:00',
        status: ReservationStatus.Active,
        cancelledAt: null,
        guestName: null,
        rowVersion: 'AAA='
      }
    ]);
    api.weekResponse = of(sampleWeek);
    await service.loadWeek(weekStart);
    await service.loadMine();

    const result = await service.cancel('r1', 'AAA=');

    expect(result).toEqual({ ok: true });
    expect(service.myReservations()[0].status).toBe(ReservationStatus.Cancelled);
    expect(service.weekReservations().find(r => r.id === 'r1')).toBeUndefined();
    expect(api.cancelCalls).toEqual([{ id: 'r1', rowVersion: 'AAA=' }]);
  });

  it('cancel maps 409 to conflict without mutating state', async () => {
    const { service, api } = setup();
    api.weekResponse = of(sampleWeek);
    await service.loadWeek(weekStart);
    api.cancelResponse = throwError(() =>
      httpError(409, { error: 'Die Buchung wurde inzwischen geändert, bitte neu laden.' }));

    const result = await service.cancel('r1', 'AAA=');

    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.status).toBe('conflict');
    expect(service.weekReservations()).toEqual(sampleWeek);
  });
});
