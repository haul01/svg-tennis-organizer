import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CreateMemberRequest,
  ListMembersOptions,
  MemberDetailDto,
  MemberListItemDto,
  MemberRole,
  UpdateMemberRequest
} from '../models/member.model';

@Injectable({ providedIn: 'root' })
export class MembersApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/members`;

  list(opts: ListMembersOptions = {}): Observable<MemberListItemDto[]> {
    let params = new HttpParams();
    if (opts.search) params = params.set('search', opts.search);
    if (opts.status) params = params.set('status', opts.status);
    if (opts.role) params = params.set('role', opts.role);
    return this.http.get<MemberListItemDto[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<MemberDetailDto> {
    return this.http.get<MemberDetailDto>(`${this.baseUrl}/${id}`);
  }

  create(req: CreateMemberRequest): Observable<MemberDetailDto> {
    return this.http.post<MemberDetailDto>(this.baseUrl, req);
  }

  update(id: string, req: UpdateMemberRequest): Observable<MemberDetailDto> {
    return this.http.put<MemberDetailDto>(`${this.baseUrl}/${id}`, req);
  }

  setActive(id: string, isActive: boolean): Observable<MemberDetailDto> {
    return this.http.post<MemberDetailDto>(
      `${this.baseUrl}/${id}/set-active`,
      { isActive }
    );
  }

  triggerPasswordReset(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/reset-password`, {});
  }

  changeRole(id: string, role: MemberRole): Observable<MemberDetailDto> {
    return this.http.post<MemberDetailDto>(
      `${this.baseUrl}/${id}/role`,
      { role }
    );
  }

  importCsv(file: File): Observable<ImportCsvSummary> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<ImportCsvSummary>(`${this.baseUrl}/import`, form);
  }
}

export interface ImportCsvRowError {
  lineNumber: number;
  email: string | null;
  message: string;
}

export interface ImportCsvSummary {
  totalRows: number;
  created: number;
  skippedEmails: string[];
  failed: ImportCsvRowError[];
}
