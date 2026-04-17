import { HttpClient, HttpContext } from '@angular/common/http';
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

  createBooking(slotId: number, patientName: string): Observable<BookingApiDto> {
    return this.http.post<BookingApiDto>(
      `${environment.apiUrl}/bookings`,
      { slot_id: slotId, patient_name: patientName.trim() },
      { context: this.context },
    );
  }

  cancelBooking(bookingId: number): Observable<unknown> {
    return this.http.delete(`${environment.apiUrl}/bookings/${bookingId}`, { context: this.context });
  }
}
