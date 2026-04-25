import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CourtBlockDto,
  CreateCourtBlockOnceRequest,
  CreateCourtBlockSeriesRequest,
  CreateOnceResponse,
  CreateSeriesResponse
} from '../models/court-block.model';

@Injectable({ providedIn: 'root' })
export class CourtBlocksApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/court-blocks`;

  list(opts: { from?: string; to?: string; courtId?: number } = {}): Observable<CourtBlockDto[]> {
    let params = new HttpParams();
    if (opts.from) params = params.set('from', opts.from);
    if (opts.to) params = params.set('to', opts.to);
    if (opts.courtId !== undefined) params = params.set('courtId', String(opts.courtId));
    return this.http.get<CourtBlockDto[]>(this.baseUrl, { params });
  }

  forWeek(weekStart: Date): Observable<CourtBlockDto[]> {
    const params = new HttpParams().set('startDate', weekStart.toISOString());
    return this.http.get<CourtBlockDto[]>(`${this.baseUrl}/week`, { params });
  }

  createOnce(req: CreateCourtBlockOnceRequest): Observable<CreateOnceResponse> {
    return this.http.post<CreateOnceResponse>(this.baseUrl, req);
  }

  createSeries(req: CreateCourtBlockSeriesRequest): Observable<CreateSeriesResponse> {
    return this.http.post<CreateSeriesResponse>(`${this.baseUrl}/series`, req);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  deleteSeries(seriesId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/series/${seriesId}`);
  }
}
