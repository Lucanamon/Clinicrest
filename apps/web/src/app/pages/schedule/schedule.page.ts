import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BookingApiDto } from '../../booking/booking-api.types';
import { BookingService } from '../../booking/booking.service';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-schedule-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './schedule.page.html',
  styleUrl: './schedule.page.scss'
})
export class SchedulePage implements OnInit {
  private readonly bookingService = inject(BookingService);
  readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly scheduledRows = signal<BookingApiDto[]>([]);

  private readonly phonePattern = /^[0-9+]*$/;

  ngOnInit(): void {
    this.loadScheduled();
  }

  get doctorLabel(): string {
    const profile = this.auth.getCurrentUserProfile();
    return profile?.displayName?.trim() || this.auth.getUsername();
  }

  formatDateTime(iso?: string | null): string {
    if (!iso) {
      return 'N/A';
    }
    const d = new Date(iso);
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(d);
  }

  getValidPhone(raw?: string | null): string {
    const value = (raw ?? '').trim();
    if (!value) {
      return 'Not provided';
    }
    return this.phonePattern.test(value) ? value : 'Invalid phone format';
  }

  cancelScheduledAppointment(row: BookingApiDto): void {
    const ok = window.confirm(`Cancel appointment for ${row.patient_name}? This will free slot capacity.`);
    if (!ok) {
      return;
    }

    this.bookingService
      .cancelBooking(row.id)
      .subscribe({
        next: () => this.loadScheduled(),
        error: () => {
          this.error.set('Could not cancel scheduled appointment.');
        }
      });
  }

  hasUnsavedChanges(): boolean {
    return false;
  }

  private loadScheduled(): void {
    this.loading.set(true);
    this.error.set(null);
    this.bookingService.getBookings('SCHEDULED').subscribe({
      next: (rows) => {
        this.scheduledRows.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load scheduled appointments.');
        this.loading.set(false);
      }
    });
  }
}
