import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { BookingStateService } from '../../booking/booking-state.service';
import { UtcToLocalPipe } from '../../booking/utc-to-local.pipe';

const GUEST_PHONE_STORAGE_KEY = 'clinicrest.guestPhone';

function utcTodayYmd(): string {
  const now = new Date();
  const y = now.getUTCFullYear();
  const m = String(now.getUTCMonth() + 1).padStart(2, '0');
  const d = String(now.getUTCDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, UtcToLocalPipe],
  templateUrl: './register.page.html',
  styleUrl: './register.page.scss',
})
export class RegisterPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  readonly booking = inject(BookingStateService);

  readonly form = this.fb.nonNullable.group({
    phoneNumber: ['', [Validators.required, Validators.pattern(/^\d+$/)]],
    dateYmd: [utcTodayYmd(), [Validators.required]],
    slotId: ['', [Validators.required]],
  });

  ngOnInit(): void {
    const storedPhone = this.getStoredGuestPhone();
    if (storedPhone) {
      this.form.controls.phoneNumber.setValue(storedPhone);
    }

    this.booking.setSelectedDateYmd(this.form.controls.dateYmd.getRawValue());

    this.form.controls.dateYmd.valueChanges.subscribe((d) => {
      if (d) {
        this.booking.setSelectedDateYmd(d);
        this.form.controls.slotId.setValue('');
      }
    });
  }

  submit(): void {
    if (this.form.invalid || this.booking.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    const { phoneNumber, slotId } = this.form.getRawValue();
    this.booking.resetBookingError();

    this.booking.bookSlot(slotId, { phoneNumber }).subscribe({
      next: () => {
        this.storeGuestPhone(phoneNumber);
        void this.router.navigate(['/booking'], { queryParams: { phone: phoneNumber } });
      },
      error: () => undefined,
    });
  }

  private storeGuestPhone(phoneNumber: string): void {
    if (typeof window === 'undefined') {
      return;
    }

    const normalized = phoneNumber.trim();
    if (!normalized) {
      return;
    }

    window.localStorage.setItem(GUEST_PHONE_STORAGE_KEY, normalized);
  }

  private getStoredGuestPhone(): string | null {
    if (typeof window === 'undefined') {
      return null;
    }

    const raw = window.localStorage.getItem(GUEST_PHONE_STORAGE_KEY)?.trim();
    return raw ? raw : null;
  }
}
