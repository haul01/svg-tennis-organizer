import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ListReservationsQuery,
  ListReservationsResponse
} from '../models/report.model';

@Injectable({ providedIn: 'root' })
export class ReportsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/admin/reports`;

  listReservations(q: ListReservationsQuery = {}): Observable<ListReservationsResponse> {
    let params = new HttpParams();
    if (q.from) params = params.set('from', q.from.toISOString());
    if (q.to) params = params.set('to', q.to.toISOString());
    if (q.courtId !== undefined) params = params.set('courtId', q.courtId);
    if (q.status !== undefined) params = params.set('status', q.status);
    if (q.page) params = params.set('page', q.page);
    if (q.pageSize) params = params.set('pageSize', q.pageSize);
    return this.http.get<ListReservationsResponse>(
      `${this.baseUrl}/reservations`, { params });
  }
}
