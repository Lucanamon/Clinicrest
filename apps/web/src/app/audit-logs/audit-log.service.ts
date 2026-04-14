import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AuditLogDto {
  id: string;
  userId: string;
  actorUsername: string | null;
  action: string;
  entityName: string;
  entityId: string;
  oldValues: string | null;
  newValues: string | null;
  timestamp: string;
}

export interface PagedAuditLogs {
  items: AuditLogDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuditLogService {
  constructor(private readonly http: HttpClient) {}

  getPaged(pageNumber: number, pageSize: number): Observable<PagedAuditLogs> {
    const params = new HttpParams()
      .set('pageNumber', String(pageNumber))
      .set('pageSize', String(pageSize));
    return this.http.get<PagedAuditLogs>(`${environment.apiUrl}/audit-logs`, { params });
  }
}
