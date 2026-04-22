import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BookingApiDto } from '../../booking/booking-api.types';
import { BookingService } from '../../booking/booking.service';
import { AuthService } from '../../services/auth';
import { NotificationTestService } from '../../services/notification-test.service';

@Component({
  selector: 'app-schedule-page',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './schedule.page.html',
  styleUrl: './schedule.page.scss'
})
export class SchedulePage implements OnInit {
  private readonly bookingService = inject(BookingService);
  private readonly notificationTest = inject(NotificationTestService);
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly scheduledRows = signal<BookingApiDto[]>([]);
  /** Booking id while a manual reminder retry is in flight */
  readonly retryingBookingId = signal<number | null>(null);

  readonly testPlayspaceOpen = signal(false);
  readonly testSendLoading = signal(false);
  readonly testSendFeedback = signal<{ kind: 'ok' | 'err'; text: string } | null>(null);

  readonly testForm = this.fb.nonNullable.group({
    phoneNumber: ['+1234567890', [Validators.required, Validators.minLength(1)]],
    message: ['Test alert from Notification Playspace', [Validators.required, Validators.minLength(1)]],
    channel: this.fb.nonNullable.control<'Sms' | 'Email'>('Sms', { validators: [Validators.required] })
  });

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

  /**
   * Normalized reminder state for the latest notification job (API returns e.g. Sent, Pending, Failed).
   */
  reminderKind(
    row: BookingApiDto
  ): 'sent' | 'pending' | 'retrying' | 'failed' | 'cancelled' | 'none' {
    const raw = (row.notificationStatus ?? '').trim();
    if (!raw) {
      return 'none';
    }
    const key = raw.toLowerCase();
    if (key === 'sent') {
      return 'sent';
    }
    if (key === 'pending') {
      return 'pending';
    }
    if (key === 'retrying') {
      return 'retrying';
    }
    if (key === 'failed') {
      return 'failed';
    }
    if (key === 'cancelled') {
      return 'cancelled';
    }
    return 'none';
  }

  failedTooltip(row: BookingApiDto): string {
    return (row.lastError ?? '').trim() || 'Reminder could not be sent. Check the mock SMS service logs.';
  }

  retryFailedReminder(row: BookingApiDto): void {
    this.retryingBookingId.set(row.id);
    this.error.set(null);
    this.bookingService.retryFailedNotification(row.id).subscribe({
      next: () => {
        this.retryingBookingId.set(null);
        this.loadScheduled();
      },
      error: () => {
        this.retryingBookingId.set(null);
        this.error.set('Could not queue reminder retry. Try again or contact support.');
      }
    });
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

  openTestPlayspace(): void {
    this.testSendFeedback.set(null);
    this.testPlayspaceOpen.set(true);
  }

  closeTestPlayspace(): void {
    this.testPlayspaceOpen.set(false);
    this.testSendLoading.set(false);
  }

  sendTestNotification(): void {
    if (this.testForm.invalid) {
      this.testForm.markAllAsTouched();
      return;
    }
    const { phoneNumber, message, channel } = this.testForm.getRawValue();
    this.testSendLoading.set(true);
    this.testSendFeedback.set(null);
    this.notificationTest.testSend({ phoneNumber, message, channel }).subscribe({
      next: (res) => {
        this.testSendLoading.set(false);
        this.testSendFeedback.set({ kind: 'ok', text: res.message });
      },
      error: (err) => {
        this.testSendLoading.set(false);
        const msg =
          err?.error?.message ??
          err?.error?.title ??
          (typeof err?.message === 'string' ? err.message : 'Request failed. Are you signed in?');
        this.testSendFeedback.set({ kind: 'err', text: String(msg) });
      }
    });
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
