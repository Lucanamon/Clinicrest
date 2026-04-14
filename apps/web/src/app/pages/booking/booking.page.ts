import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BookingStateService } from '../../booking/booking-state.service';
import { UtcToLocalPipe } from '../../booking/utc-to-local.pipe';

@Component({
  selector: 'app-booking-page',
  standalone: true,
  imports: [CommonModule, FormsModule, UtcToLocalPipe],
  templateUrl: './booking.page.html',
  styleUrl: './booking.page.scss',
})
export class BookingPage implements OnInit {
  readonly booking = inject(BookingStateService);

  ngOnInit(): void {
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
    this.booking.bookSlot(slotId).subscribe({ error: () => undefined });
  }
}
