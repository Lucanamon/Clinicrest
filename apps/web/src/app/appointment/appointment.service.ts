import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { PagedResult } from '../patient/patient.service';

export interface AppointmentDto {
  id: string;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  appointmentDate: string;
  status: string;
  notes: string | null;
  createdAt: string;
}

export interface CreateAppointmentRequest {
  patientId: string;
  doctorId?: string | null;
  appointmentDate: string;
  status: string;
  notes?: string | null;
}

export type UpdateAppointmentRequest = CreateAppointmentRequest;

export interface GetAppointmentsParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  status?: string;
  fromAppointmentDate?: string;
  toAppointmentDate?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

@Injectable({
  providedIn: 'root'
})
export class AppointmentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/appointments';

  private readonly searchTerm$ = new BehaviorSubject<string>('');
  private readonly sortBy$ = new BehaviorSubject<string>('AppointmentDate');
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

  getPaged(params?: GetAppointmentsParams): Observable<PagedResult<AppointmentDto>> {
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
    if (params?.fromAppointmentDate) {
      httpParams = httpParams.set('fromAppointmentDate', params.fromAppointmentDate);
    }
    if (params?.toAppointmentDate) {
      httpParams = httpParams.set('toAppointmentDate', params.toAppointmentDate);
    }
    const sortBy = params?.sortBy;
    const sortDirection = params?.sortDirection;
    if (sortBy && sortDirection) {
      const isDefault = sortBy === 'AppointmentDate' && sortDirection === 'desc';
      if (!isDefault) {
        httpParams = httpParams.set('sortBy', sortBy);
        httpParams = httpParams.set('sortDirection', sortDirection);
      }
    }
    return this.http.get<PagedResult<AppointmentDto>>(this.baseUrl, { params: httpParams });
  }

  getById(id: string): Observable<AppointmentDto> {
    return this.http.get<AppointmentDto>(`${this.baseUrl}/${id}`);
  }

  create(data: CreateAppointmentRequest): Observable<AppointmentDto> {
    return this.http.post<AppointmentDto>(this.baseUrl, data);
  }

  update(id: string, data: UpdateAppointmentRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
