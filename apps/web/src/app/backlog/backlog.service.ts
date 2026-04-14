import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PagedResult } from '../patient/patient.service';

export interface BacklogDto {
  id: string;
  title: string;
  description: string | null;
  priority: string;
  status: string;
  assignedToUserId: string;
  assignedToName: string;
  createdAt: string;
}

export interface CreateBacklogRequest {
  title: string;
  description?: string | null;
  priority: string;
  status: string;
  assignedToUserId: string;
}

export type UpdateBacklogRequest = CreateBacklogRequest;

export interface GetBacklogsParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  status?: string;
  priority?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

@Injectable({
  providedIn: 'root'
})
export class BacklogService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/backlogs`;

  private readonly searchTerm$ = new BehaviorSubject<string>('');
  private readonly sortBy$ = new BehaviorSubject<string>('CreatedAt');
  private readonly sortDirection$ = new BehaviorSubject<'asc' | 'desc'>('desc');

  setSearchTerm(term: string): void {
    this.searchTerm$.next(term);
  }

  getSearchTerm(): Observable<string> {
    return this.searchTerm$.asObservable();
  }

  getSearchTermSnapshot(): string {
    return this.searchTerm$.value;
  }

  setSort(sortBy: string, sortDirection: 'asc' | 'desc'): void {
    this.sortBy$.next(sortBy);
    this.sortDirection$.next(sortDirection);
  }

  getSortBy(): Observable<string> {
    return this.sortBy$.asObservable();
  }

  getSortDirection(): Observable<'asc' | 'desc'> {
    return this.sortDirection$.asObservable();
  }

  getSortBySnapshot(): string {
    return this.sortBy$.value;
  }

  getSortDirectionSnapshot(): 'asc' | 'desc' {
    return this.sortDirection$.value;
  }

  getPaged(params?: GetBacklogsParams): Observable<PagedResult<BacklogDto>> {
    let httpParams = new HttpParams();
    if (params?.pageNumber != null) {
      httpParams = httpParams.set('pageNumber', String(params.pageNumber));
    }
    if (params?.pageSize != null) {
      httpParams = httpParams.set('pageSize', String(params.pageSize));
    }
    if (params?.searchTerm?.trim()) {
      httpParams = httpParams.set('searchTerm', params.searchTerm.trim());
    }
    if (params?.status?.trim()) {
      httpParams = httpParams.set('status', params.status.trim());
    }
    if (params?.priority?.trim()) {
      httpParams = httpParams.set('priority', params.priority.trim());
    }
    const sortBy = params?.sortBy;
    const sortDirection = params?.sortDirection;
    if (sortBy && sortDirection) {
      const isDefault = sortBy === 'CreatedAt' && sortDirection === 'desc';
      if (!isDefault) {
        httpParams = httpParams.set('sortBy', sortBy);
        httpParams = httpParams.set('sortDirection', sortDirection);
      }
    }
    return this.http.get<PagedResult<BacklogDto>>(this.baseUrl, { params: httpParams });
  }

  getById(id: string): Observable<BacklogDto> {
    return this.http.get<BacklogDto>(`${this.baseUrl}/${id}`);
  }

  create(data: CreateBacklogRequest): Observable<BacklogDto> {
    return this.http.post<BacklogDto>(this.baseUrl, data);
  }

  update(id: string, data: UpdateBacklogRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
