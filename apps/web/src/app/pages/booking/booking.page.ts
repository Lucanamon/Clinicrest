import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BookingStateService } from '../../booking/booking-state.service';
import { UtcToLocalPipe } from '../../booking/utc-to-local.pipe';

const GUEST_PHONE_STORAGE_KEY = 'clinicrest.guestPhone';

@Component({
  selector: 'app-booking-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, UtcToLocalPipe],
  templateUrl: './booking.page.html',
  styleUrl: './booking.page.scss',
})
export class BookingPage implements OnInit {
  readonly booking = inject(BookingStateService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  /** When set (e.g. from /booking?phone=... after guest registration), bookings use phoneNumber instead of user_id. */
  guestPhone: string | null = null;
  enteredPhone = '';

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((q) => {
      const fromQuery = q.get('phone')?.trim() || null;
      const resolvedPhone = fromQuery || this.getStoredGuestPhone();

      if (!fromQuery && resolvedPhone) {
        void this.router.navigate([], {
          relativeTo: this.route,
          queryParams: { phone: resolvedPhone },
          queryParamsHandling: 'merge',
          replaceUrl: true,
        });
      }

      this.guestPhone = resolvedPhone;
      this.enteredPhone = resolvedPhone ?? '';
      const phone = this.guestPhone?.trim();
      if (phone) {
        this.storeGuestPhone(phone);
        this.booking.loadBookingsByPhone(phone);
      } else {
        this.booking.clearPhoneBookings();
      }
    });
    this.booking.loadSlots();
  }

  onDateInput(value: string): void {
    if (value) {
      this.booking.setSelectedDateYmd(value);
    }
  }

  refresh(): void {
    this.booking.loadSlots();
  }

  book(slotId: string): void {
    this.booking.resetBookingError();
    const phone = this.guestPhone?.trim() || undefined;
    this.booking.bookSlot(slotId, phone ? { phoneNumber: phone } : undefined).subscribe({
      next: () => {
        if (phone) {
          this.storeGuestPhone(phone);
          this.booking.loadBookingsByPhone(phone);
        }
      },
      error: () => undefined,
    });
  }

  cancelBooking(bookingId: string): void {
    const phone = this.guestPhone?.trim();
    if (!phone) {
      return;
    }
    this.booking.cancelBooking(bookingId, phone).subscribe({ error: () => undefined });
  }

  applyPhone(): void {
    const normalized = this.enteredPhone.trim();
    if (!normalized) {
      return;
    }

    this.storeGuestPhone(normalized);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { phone: normalized },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  private storeGuestPhone(phoneNumber: string): void {
    if (typeof window === 'undefined') {
      return;
    }

    window.localStorage.setItem(GUEST_PHONE_STORAGE_KEY, phoneNumber.trim());
  }

  private getStoredGuestPhone(): string | null {
    if (typeof window === 'undefined') {
      return null;
    }

    const value = window.localStorage.getItem(GUEST_PHONE_STORAGE_KEY)?.trim();
    return value ? value : null;
  }
}
