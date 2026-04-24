import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PublicSettingsDto, UpdateSettingsRequest } from '../models/settings.model';

@Injectable({ providedIn: 'root' })
export class SettingsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/settings`;

  getPublic(): Observable<PublicSettingsDto> {
    return this.http.get<PublicSettingsDto>(`${this.baseUrl}/public`);
  }

  update(req: UpdateSettingsRequest): Observable<PublicSettingsDto> {
    return this.http.put<PublicSettingsDto>(this.baseUrl, req);
  }
}
