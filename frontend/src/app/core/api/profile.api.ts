import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ChangePasswordRequest,
  ProfileDto,
  UpdateProfileRequest
} from '../models/profile.model';

@Injectable({ providedIn: 'root' })
export class ProfileApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/profile`;

  get(): Observable<ProfileDto> {
    return this.http.get<ProfileDto>(this.baseUrl);
  }

  update(req: UpdateProfileRequest): Observable<ProfileDto> {
    return this.http.put<ProfileDto>(this.baseUrl, req);
  }

  changePassword(req: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/change-password`, req);
  }
}
