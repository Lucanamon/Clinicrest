import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { skipGlobalErrorAlert } from '../interceptors/http-context.tokens';
import type { BookingApiDto, SlotApiDto } from './booking-api.types';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);
  private readonly context = new HttpContext().set(skipGlobalErrorAlert, true);

  getSlots(): Observable<SlotApiDto[]> {
    return this.http.get<SlotApiDto[]>(`${environment.apiUrl}/slots`, { context: this.context });
  }

  createBooking(slotId: number, patientName: string, phoneNumber?: string | null): Observable<BookingApiDto> {
    return this.http.post<BookingApiDto>(
      `${environment.apiUrl}/bookings`,
      { slot_id: slotId, patient_name: patientName.trim(), phone_number: phoneNumber?.trim() || null },
      { context: this.context },
    );
  }

  cancelBooking(bookingId: number): Observable<unknown> {
    return this.http.delete(`${environment.apiUrl}/bookings/${bookingId}`, { context: this.context });
  }

  scheduleBooking(bookingId: number, patientId: string): Observable<unknown> {
    return this.http.put(
      `${environment.apiUrl}/bookings/${bookingId}/schedule`,
      { patient_id: patientId },
      { context: this.context }
    );
  }

  getBookings(status: 'ACTIVE' | 'SCHEDULED' | 'CANCELLED' = 'ACTIVE'): Observable<BookingApiDto[]> {
    const params = new HttpParams().set('status', status);
    return this.http.get<BookingApiDto[]>(`${environment.apiUrl}/bookings`, { params, context: this.context });
  }
}
