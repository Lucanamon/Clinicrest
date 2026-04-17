import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, finalize, map, tap } from 'rxjs/operators';
import { skipGlobalErrorAlert } from '../interceptors/http-context.tokens';
import { environment } from '../../environments/environment';
import type { BookingApiDto, CreateTimeSlotRequest, SlotApiDto, UpdateTimeSlotCapacityAction } from './booking-api.types';

function utcTodayYmd(): string {
  const now = new Date();
  const y = now.getUTCFullYear();
  const m = String(now.getUTCMonth() + 1).padStart(2, '0');
  const d = String(now.getUTCDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function httpAlertContext(): HttpContext {
  return new HttpContext().set(skipGlobalErrorAlert, true);
}

function looksLikeHtmlDocument(s: string): boolean {
  const lead = s.trimStart();
  return lead.startsWith('<!DOCTYPE') || lead.toLowerCase().startsWith('<html');
}

function parseApiError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return 'Network error. Check your connection and try again.';
    }
    const payload = error.error as { message?: string } | string | null | undefined;
    if (typeof payload === 'string' && payload.trim() !== '') {
      if (looksLikeHtmlDocument(payload)) {
        return 'The server returned HTML instead of JSON. Check that /api routes reach the API, not the SPA.';
      }
      if (payload.length > 400 && payload.includes('<')) {
        return 'The server returned a non-JSON response (likely HTML). Check the API proxy and backend.';
      }
      return payload;
    }
    if (payload && typeof payload === 'object' && typeof payload.message === 'string' && payload.message.trim() !== '') {
      return payload.message;
    }
    if (error.status === 409) {
      return 'Slot Full';
    }
    if (error.status === 400) {
      return 'Booking was rejected. Check your details and try again.';
    }
    const msg = typeof error.message === 'string' ? error.message : '';
    if (msg.includes('Http failure during parsing')) {
      return 'Invalid JSON from the API (response may be HTML or malformed JSON).';
    }
    return `Request failed (${error.status || 0}).`;
  }

  if (error instanceof Error) {
    const m = error.message.trim();
    return m !== '' ? m : 'An unexpected error occurred.';
  }

  return 'An unexpected error occurred.';
}

function normalizeSlotsLoadError(err: unknown): unknown {
  if (!(err instanceof HttpErrorResponse)) {
    return err;
  }

  const raw = err.error;
  if (typeof raw === 'string') {
    console.log('RAW RESPONSE:', raw);
    if (looksLikeHtmlDocument(raw)) {
      return new Error('Backend returned HTML instead of JSON');
    }
  }

  const msg = typeof err.message === 'string' ? err.message : '';
  if (msg.includes('Http failure during parsing')) {
    return new Error('Invalid JSON from slots API (response may be HTML or malformed JSON).');
  }

  return err;
}

@Injectable({ providedIn: 'root' })
export class BookingStateService {
  private readonly http = inject(HttpClient);

  private readonly _slots = signal<SlotApiDto[]>([]);
  private readonly _loading = signal(false);
  private readonly _listError = signal<string | null>(null);
  private readonly _bookingError = signal<string | null>(null);
  private readonly _slotCreateError = signal<string | null>(null);
  private readonly _creatingSlot = signal(false);
  private readonly _capacityAdjustingSlotId = signal<number | null>(null);
  private readonly _deletingSlotId = signal<number | null>(null);
  private readonly _bookingSlotId = signal<number | null>(null);
  private readonly _selectedDateYmd = signal<string>(utcTodayYmd());

  readonly slots = this._slots.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly listError = this._listError.asReadonly();
  readonly bookingError = this._bookingError.asReadonly();
  readonly slotCreateError = this._slotCreateError.asReadonly();
  readonly creatingSlot = this._creatingSlot.asReadonly();
  readonly capacityAdjustingSlotId = this._capacityAdjustingSlotId.asReadonly();
  readonly deletingSlotId = this._deletingSlotId.asReadonly();
  readonly bookingSlotId = this._bookingSlotId.asReadonly();
  readonly selectedDateYmd = this._selectedDateYmd.asReadonly();

  setSelectedDateYmd(value: string): void {
    this._selectedDateYmd.set(value);
    this.loadSlots();
  }

  resetBookingError(): void {
    this._bookingError.set(null);
  }

  resetSlotCreateError(): void {
    this._slotCreateError.set(null);
  }

  loadSlots(options?: { silent?: boolean }): void {
    const silent = options?.silent ?? false;
    const date = this._selectedDateYmd();
    if (!silent) {
      this._loading.set(true);
    }
    this._listError.set(null);

    this.http
      .get<unknown>(`${environment.apiUrl}/slots`, {
        params: { date },
        responseType: 'json',
        context: httpAlertContext(),
      })
      .pipe(
        map((body) => {
          if (!environment.production) {
            console.log('RAW RESPONSE:', JSON.stringify(body));
          }
          if (!Array.isArray(body)) {
            console.log('RAW RESPONSE:', body);
            throw new Error('Slots API returned invalid JSON (expected a JSON array of slots).');
          }
          return body as SlotApiDto[];
        }),
        catchError((err: unknown) => throwError(() => normalizeSlotsLoadError(err))),
        finalize(() => {
          if (!silent) {
            this._loading.set(false);
          }
        }),
      )
      .subscribe({
        next: (rows) => this._slots.set(rows),
        error: (err: unknown) => this._listError.set(parseApiError(err)),
      });
  }

  loadAllSlots(options?: { silent?: boolean }): void {
    const silent = options?.silent ?? false;
    if (!silent) {
      this._loading.set(true);
    }
    this._listError.set(null);

    this.http
      .get<unknown>(`${environment.apiUrl}/slots`, {
        responseType: 'json',
        context: httpAlertContext(),
      })
      .pipe(
        map((body) => {
          if (!Array.isArray(body)) {
            throw new Error('Slots API returned invalid JSON (expected a JSON array of slots).');
          }
          return body as SlotApiDto[];
        }),
        catchError((err: unknown) => throwError(() => normalizeSlotsLoadError(err))),
        finalize(() => {
          if (!silent) {
            this._loading.set(false);
          }
        }),
      )
      .subscribe({
        next: (rows) => this._slots.set(rows),
        error: (err: unknown) => this._listError.set(parseApiError(err)),
      });
  }

  createSlot(payload: CreateTimeSlotRequest): Observable<SlotApiDto> {
    this._slotCreateError.set(null);
    this._creatingSlot.set(true);

    return this.http
      .post<SlotApiDto>(`${environment.apiUrl}/time-slots`, payload, { context: httpAlertContext() })
      .pipe(
        tap(() => this.loadAllSlots({ silent: true })),
        catchError((err: unknown) => {
          this._slotCreateError.set(parseApiError(err));
          return throwError(() => err);
        }),
        finalize(() => this._creatingSlot.set(false)),
      );
  }

  updateSlotCapacity(slotId: number, action: UpdateTimeSlotCapacityAction): Observable<SlotApiDto> {
    this._slotCreateError.set(null);
    this._capacityAdjustingSlotId.set(slotId);

    return this.http
      .patch<SlotApiDto>(`${environment.apiUrl}/time-slots/${slotId}/capacity`, { action }, { context: httpAlertContext() })
      .pipe(
        tap((updated) => {
          this._slots.update((list) => list.map((slot) => (slot.id === updated.id ? updated : slot)));
        }),
        catchError((err: unknown) => {
          this._slotCreateError.set(parseApiError(err));
          return throwError(() => err);
        }),
        finalize(() => this._capacityAdjustingSlotId.set(null)),
      );
  }

  deleteSlot(slotId: number): Observable<void> {
    this._slotCreateError.set(null);
    this._deletingSlotId.set(slotId);

    return this.http
      .delete<void>(`${environment.apiUrl}/time-slots/${slotId}`, { context: httpAlertContext() })
      .pipe(
        tap(() => this.loadAllSlots({ silent: true })),
        catchError((err: unknown) => {
          this._slotCreateError.set(parseApiError(err));
          return throwError(() => err);
        }),
        finalize(() => this._deletingSlotId.set(null)),
      );
  }

  /**
   * Queue booking: POST /api/bookings with patient_name and slot_id (UTC slot times; server enforces capacity).
   */
  bookSlot(slotId: number, patientName: string): Observable<BookingApiDto> {
    const name = patientName.trim();
    if (!name) {
      this._bookingError.set('Patient name is required.');
      return throwError(() => new Error('Patient name is required.'));
    }

    const slot = this._slots().find((s) => s.id === slotId);
    if (!slot || slot.available_slots <= 0) {
      this._bookingError.set('This slot has no availability.');
      return throwError(() => new Error('This slot has no availability.'));
    }

    this._bookingError.set(null);
    this._bookingSlotId.set(slotId);
    const snapshot = this._slots().map((s) => ({ ...s }));

    this.applyOptimistic(slotId);

    return this.http
      .post<BookingApiDto>(
        `${environment.apiUrl}/bookings`,
        { patient_name: name, slot_id: slotId },
        { context: httpAlertContext() },
      )
      .pipe(
        tap(() => this.loadSlots({ silent: true })),
        catchError((err: unknown) => {
          this._slots.set(snapshot);
          this._bookingError.set(parseApiError(err));
          this.loadSlots({ silent: true });
          return throwError(() => err);
        }),
        finalize(() => this._bookingSlotId.set(null)),
      );
  }

  private applyOptimistic(slotId: number): void {
    this._slots.update((list) =>
      list.map((s) =>
        s.id === slotId
          ? {
              ...s,
              available_slots: Math.max(0, s.available_slots - 1),
              booked_count: Math.min(s.capacity, s.booked_count + 1),
            }
          : s,
      ),
    );
  }
}
