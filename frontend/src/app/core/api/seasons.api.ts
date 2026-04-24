import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SeasonDto, UpdateSeasonRequest } from '../models/season.model';

@Injectable({ providedIn: 'root' })
export class SeasonsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/seasons`;

  /**
   * 204 No Content when no season is active today. Observable emits
   * `null` in that case so callers can render a dedicated empty state.
   */
  current(): Observable<SeasonDto | null> {
    return this.http.get<SeasonDto | null>(`${this.baseUrl}/current`, {
      observe: 'body',
      responseType: 'json'
    });
  }

  update(id: number, req: UpdateSeasonRequest): Observable<SeasonDto> {
    return this.http.put<SeasonDto>(`${this.baseUrl}/${id}`, req);
  }
}
