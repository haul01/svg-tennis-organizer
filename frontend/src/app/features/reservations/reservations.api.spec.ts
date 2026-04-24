import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { ReservationStatus } from './reservation.model';
import { ReservationsApi } from './reservations.api';

describe('ReservationsApi', () => {
  let api: ReservationsApi;
  let httpMock: HttpTestingController;

  const baseUrl = `${environment.apiUrl}/reservations`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    api = TestBed.inject(ReservationsApi);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getWeek sends startDate as ISO', () => {
    const weekStart = new Date('2026-06-08T00:00:00.000Z');
    api.getWeek(weekStart).subscribe();

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/week`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('startDate')).toBe(weekStart.toISOString());
    req.flush([]);
  });

  it('getMine forwards upcomingOnly and status filters', () => {
    api.getMine({ upcomingOnly: true, status: ReservationStatus.Active }).subscribe();

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/mine`);
    expect(req.request.params.get('upcomingOnly')).toBe('true');
    expect(req.request.params.get('status')).toBe('Active');
    req.flush([]);
  });

  it('getMine omits filters when not provided', () => {
    api.getMine().subscribe();

    const req = httpMock.expectOne(r => r.url === `${baseUrl}/mine`);
    expect(req.request.params.has('upcomingOnly')).toBe(false);
    expect(req.request.params.has('status')).toBe(false);
    req.flush([]);
  });

  it('create POSTs the payload to the base URL', () => {
    const body = {
      courtId: 1,
      startsAt: '2026-06-10T18:00:00+02:00',
      endsAt: '2026-06-10T19:00:00+02:00',
      guestPlayerId: null
    };
    api.create(body).subscribe();

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ id: 'abc' });
  });

  it('cancel wraps the base64 rowVersion in a quoted If-Match header', () => {
    api.cancel('reservation-id', 'QUFB').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/reservation-id`);
    expect(req.request.method).toBe('DELETE');
    expect(req.request.headers.get('If-Match')).toBe('"QUFB"');
    req.flush(null);
  });
});
