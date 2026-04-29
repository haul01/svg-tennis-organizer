import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CreateReservationRequest,
  CreateReservationResponse,
  ListMineOptions,
  MyReservationDto,
  ReservationStatus,
  WeekReservationDto
} from './reservation.model';

/**
 * Thin HTTP wrapper. No state, no caching, no error mapping - that lives
 * in ReservationsService. Keeps the client-server contract swappable.
 */
@Injectable({ providedIn: 'root' })
export class ReservationsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/reservations`;

  getWeek(weekStart: Date): Observable<WeekReservationDto[]> {
    const params = new HttpParams().set('startDate', weekStart.toISOString());
    return this.http.get<WeekReservationDto[]>(`${this.baseUrl}/week`, { params });
  }

  getMine(opts: ListMineOptions = {}): Observable<MyReservationDto[]> {
    let params = new HttpParams();
    if (opts.upcomingOnly) params = params.set('upcomingOnly', 'true');
    if (opts.status !== undefined) {
      params = params.set('status', ReservationStatus[opts.status]);
    }
    return this.http.get<MyReservationDto[]>(`${this.baseUrl}/mine`, { params });
  }

  create(req: CreateReservationRequest): Observable<CreateReservationResponse> {
    return this.http.post<CreateReservationResponse>(this.baseUrl, req);
  }

  cancel(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
