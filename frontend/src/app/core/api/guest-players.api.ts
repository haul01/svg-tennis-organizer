import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CreateGuestPlayerRequest, GuestPlayerDto } from '../models/guest-player.model';

@Injectable({ providedIn: 'root' })
export class GuestPlayersApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/guest-players`;

  listMine(): Observable<GuestPlayerDto[]> {
    return this.http.get<GuestPlayerDto[]>(`${this.baseUrl}/mine`);
  }

  create(req: CreateGuestPlayerRequest): Observable<GuestPlayerDto> {
    return this.http.post<GuestPlayerDto>(this.baseUrl, req);
  }
}
