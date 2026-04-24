import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CourtDto } from '../models/court.model';

@Injectable({ providedIn: 'root' })
export class CourtsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/courts`;

  list(includeInactive = false): Observable<CourtDto[]> {
    let params = new HttpParams();
    if (includeInactive) params = params.set('includeInactive', 'true');
    return this.http.get<CourtDto[]>(this.baseUrl, { params });
  }
}
