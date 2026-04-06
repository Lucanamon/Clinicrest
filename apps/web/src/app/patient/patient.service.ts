import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';

export interface PatientDto {
  id: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  phoneNumber: string;
  createdAt: string;
}

export interface CreatePatientRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  phoneNumber: string;
}

export type UpdatePatientRequest = CreatePatientRequest;

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export type PagedPatientsResult = PagedResult<PatientDto>;

export interface GetPatientsParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  gender?: string;
  fromDateOfBirth?: string;
  toDateOfBirth?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

@Injectable({
  providedIn: 'root'
})
export class PatientService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/patients';

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

  getPaged(params?: GetPatientsParams): Observable<PagedPatientsResult> {
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
    if (params?.gender?.trim()) {
      httpParams = httpParams.set('gender', params.gender.trim());
    }
    if (params?.fromDateOfBirth) {
      httpParams = httpParams.set('fromDateOfBirth', params.fromDateOfBirth);
    }
    if (params?.toDateOfBirth) {
      httpParams = httpParams.set('toDateOfBirth', params.toDateOfBirth);
    }
    const sortBy = params?.sortBy;
    const sortDirection = params?.sortDirection;
    if (sortBy && sortDirection) {
      const isDefaultCreatedDesc = sortBy === 'CreatedAt' && sortDirection === 'desc';
      if (!isDefaultCreatedDesc) {
        httpParams = httpParams.set('sortBy', sortBy);
        httpParams = httpParams.set('sortDirection', sortDirection);
      }
    }
    return this.http.get<PagedPatientsResult>(this.baseUrl, { params: httpParams });
  }

  getById(id: string): Observable<PatientDto> {
    return this.http.get<PatientDto>(`${this.baseUrl}/${id}`);
  }

  create(data: CreatePatientRequest): Observable<PatientDto> {
    return this.http.post<PatientDto>(this.baseUrl, data);
  }

  update(id: string, data: UpdatePatientRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
