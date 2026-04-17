import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BookingService } from './booking.service';
import type { SlotApiDto } from './booking-api.types';
import { UtcToLocalPipe } from './utc-to-local.pipe';

@Component({
  selector: 'app-slot-list',
  standalone: true,
  imports: [CommonModule, FormsModule, UtcToLocalPipe],
  templateUrl: './slot-list.component.html',
  styleUrl: './slot-list.component.scss',
})
export class SlotListComponent implements OnInit {
  private readonly api = inject(BookingService);

  slots: SlotApiDto[] = [];
  loading = false;
  loadError: string | null = null;
  patientName = '';
  bookingMessage: string | null = null;
  bookingError: string | null = null;
  activeBookingSlotId: number | null = null;

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.loading = true;
    this.loadError = null;
    this.api.getSlots().subscribe({
      next: (rows) => {
        this.slots = rows;
        this.loading = false;
      },
      error: () => {
        this.loadError = 'Could not load slots.';
        this.loading = false;
      },
    });
  }

  book(slot: SlotApiDto): void {
    const name = this.patientName.trim();
    this.bookingMessage = null;
    this.bookingError = null;

    if (!name) {
      this.bookingError = 'Enter a patient name.';
      return;
    }

    if (slot.available_slots <= 0) {
      this.bookingError = 'Slot Full';
      return;
    }

    const startsAt = new Date(slot.start_time).getTime();
    if (startsAt < Date.now()) {
      this.bookingError = 'This slot is no longer available.';
      return;
    }

    this.activeBookingSlotId = slot.id;
    this.api.createBooking(slot.id, name).subscribe({
      next: () => {
        this.bookingMessage = 'Booking confirmed.';
        this.activeBookingSlotId = null;
        this.refresh();
      },
      error: (err: unknown) => {
        this.activeBookingSlotId = null;
        if (err instanceof HttpErrorResponse && err.status === 409) {
          const payload = err.error;
          if (payload && typeof payload === 'object' && 'message' in payload) {
            const m = (payload as { message?: string }).message;
            if (typeof m === 'string' && m.trim() !== '') {
              this.bookingError = m;
              return;
            }
          }
          this.bookingError = 'Slot Full';
          return;
        }
        if (err instanceof HttpErrorResponse && err.error && typeof err.error === 'object' && 'message' in err.error) {
          const m = (err.error as { message?: string }).message;
          if (typeof m === 'string' && m.trim() !== '') {
            this.bookingError = m;
            return;
          }
        }
        this.bookingError = 'Booking failed. Try again.';
      },
    });
  }
}
