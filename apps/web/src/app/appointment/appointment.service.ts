import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, Subject, catchError, map, switchMap, tap } from 'rxjs';
import { environment } from '../../environments/environment';
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
  source?: 'appointments' | 'bookings';
  slotId?: number;
  phoneNumber?: string | null;
  bookingId?: number;
  linkedPatientId?: string | null;
}

export interface CreateAppointmentRequest {
  patientId: string;
  doctorId?: string | null;
  appointmentDate: string;
  status: string;
  notes?: string | null;
}

export type UpdateAppointmentRequest = CreateAppointmentRequest;

export interface FinalizeAppointmentRequest {
  booking_id: number;
  patient_id: string;
  doctor_id: string;
  appointment_date: string;
  notes?: string | null;
}

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
  private readonly baseUrl = `${environment.apiUrl}/appointments`;
  private readonly bookingsUrl = `${environment.apiUrl}/bookings`;

  private readonly searchTerm$ = new BehaviorSubject<string>('');
  private readonly sortBy$ = new BehaviorSubject<string>('AppointmentDate');
  private readonly sortDirection$ = new BehaviorSubject<'asc' | 'desc'>('desc');
  private readonly refresh$ = new Subject<void>();

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

  requestRefresh(): void {
    this.refresh$.next();
  }

  getRefreshStream(): Observable<void> {
    return this.refresh$.asObservable();
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
    return this.http.get<unknown>(this.baseUrl, { params: httpParams }).pipe(
      tap((data) => console.log('Raw Data:', data)),
      map((raw) => this.normalizePagedResult(raw, params)),
      switchMap((result) => {
        if (result.items.length > 0) {
          return [result];
        }
        return this.http.get<BookingListItemDto[]>(this.bookingsUrl).pipe(
          map((bookings) => this.mapBookingsToPagedResult(bookings, params))
        );
      }),
      catchError(() =>
        this.http.get<BookingListItemDto[]>(this.bookingsUrl).pipe(
          map((bookings) => this.mapBookingsToPagedResult(bookings, params))
        )
      )
    );
  }

  private normalizePagedResult(raw: unknown, params?: GetAppointmentsParams): PagedResult<AppointmentDto> {
    if (typeof raw === 'string') {
      const trimmed = raw.trim().toLowerCase();
      if (trimmed.startsWith('<!doctype html') || trimmed.startsWith('<html')) {
        return this.raiseUnexpectedResponse('Expected JSON but received HTML. Check API route/base URL.');
      }
      return this.raiseUnexpectedResponse('Expected JSON object or array response.');
    }

    if (Array.isArray(raw)) {
      const items = raw as AppointmentDto[];
      return {
        items,
        totalCount: items.length,
        pageNumber: params?.pageNumber ?? 1,
        pageSize: params?.pageSize ?? Math.max(items.length, 1)
      };
    }

    if (!raw || typeof raw !== 'object') {
      return this.raiseUnexpectedResponse('Unexpected appointments response type.');
    }

    const candidate = raw as Partial<PagedResult<AppointmentDto>> & { items?: unknown };
    if (!Array.isArray(candidate.items)) {
      return this.raiseUnexpectedResponse('Response object missing expected "items" array.');
    }

    return {
      items: candidate.items as AppointmentDto[],
      totalCount: typeof candidate.totalCount === 'number' ? candidate.totalCount : candidate.items.length,
      pageNumber: typeof candidate.pageNumber === 'number' ? candidate.pageNumber : params?.pageNumber ?? 1,
      pageSize:
        typeof candidate.pageSize === 'number'
          ? candidate.pageSize
          : params?.pageSize ?? Math.max(candidate.items.length, 1)
    };
  }

  private raiseUnexpectedResponse(message: string): never {
    console.error('Error:', message);
    throw new Error(message);
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

  deleteBooking(id: number): Observable<unknown> {
    return this.http.delete(`${this.bookingsUrl}/${id}`);
  }

  scheduleBooking(id: number, patientId: string): Observable<unknown> {
    return this.http.put(`${this.bookingsUrl}/${id}/schedule`, { patient_id: patientId });
  }

  finalize(data: FinalizeAppointmentRequest): Observable<AppointmentDto> {
    return this.http.post<AppointmentDto>(`${this.baseUrl}/finalize`, data);
  }

  private mapBookingsToPagedResult(
    bookings: BookingListItemDto[],
    params?: GetAppointmentsParams
  ): PagedResult<AppointmentDto> {
    const mapped = bookings
      .map((booking) => ({
        id: `booking-${booking.id}`,
        patientId: '',
        patientName: booking.patient_name ?? booking.patientName ?? '',
        phoneNumber: booking.phone_number ?? booking.phoneNumber ?? null,
        doctorId: '',
        doctorName: 'Unassigned',
        appointmentDate: booking.slot_start_time ?? booking.slotStartTime ?? booking.created_at ?? booking.createdAt ?? '',
        status: booking.status,
        notes: null,
        createdAt: booking.created_at ?? booking.createdAt ?? '',
        source: 'bookings' as const,
        slotId: booking.slot_id ?? booking.slotId,
        bookingId: booking.id,
        linkedPatientId: booking.patient_id ?? booking.patientId ?? null
      }))
      .filter((row) => this.filterByParams(row, params));

    const sorted = this.sortRows(mapped, params?.sortBy, params?.sortDirection);
    const pageNumber = params?.pageNumber ?? 1;
    const pageSize = params?.pageSize ?? 10;
    const start = (pageNumber - 1) * pageSize;
    const items = sorted.slice(start, start + pageSize);

    return {
      items,
      totalCount: sorted.length,
      pageNumber,
      pageSize
    };
  }

  private filterByParams(row: AppointmentDto, params?: GetAppointmentsParams): boolean {
    const term = params?.searchTerm?.trim().toLowerCase();
    if (term && !row.patientName.toLowerCase().includes(term)) {
      return false;
    }
    const status = params?.status?.trim().toLowerCase();
    if (status && row.status.toLowerCase() !== status) {
      return false;
    }
    const from = params?.fromAppointmentDate ? new Date(params.fromAppointmentDate) : null;
    if (from && new Date(row.appointmentDate) < from) {
      return false;
    }
    const to = params?.toAppointmentDate ? new Date(params.toAppointmentDate) : null;
    if (to) {
      const toInclusive = new Date(to);
      toInclusive.setHours(23, 59, 59, 999);
      if (new Date(row.appointmentDate) > toInclusive) {
        return false;
      }
    }
    return true;
  }

  private sortRows(rows: AppointmentDto[], sortBy?: string, sortDirection?: 'asc' | 'desc'): AppointmentDto[] {
    const direction = sortDirection ?? 'desc';
    const multiplier = direction === 'asc' ? 1 : -1;
    const key = sortBy ?? 'AppointmentDate';
    return [...rows].sort((a, b) => {
      let compareA = '';
      let compareB = '';
      if (key === 'Status') {
        compareA = a.status;
        compareB = b.status;
      } else if (key === 'CreatedAt') {
        compareA = a.createdAt;
        compareB = b.createdAt;
      } else {
        compareA = a.appointmentDate;
        compareB = b.appointmentDate;
      }
      return compareA.localeCompare(compareB) * multiplier;
    });
  }
}

interface BookingListItemDto {
  id: number;
  slot_id?: number;
  slotId?: number;
  patient_name?: string;
  patientName?: string;
  phone_number?: string | null;
  phoneNumber?: string | null;
  status: string;
  created_at?: string;
  createdAt?: string;
  slot_start_time?: string | null;
  slotStartTime?: string | null;
  patient_id?: string | null;
  patientId?: string | null;
}
