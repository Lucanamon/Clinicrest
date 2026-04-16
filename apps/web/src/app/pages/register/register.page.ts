import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BookingStateService } from '../../booking/booking-state.service';
import type { SlotApiDto } from '../../booking/booking-api.types';
import { UtcToLocalPipe } from '../../booking/utc-to-local.pipe';

const GUEST_PHONE_STORAGE_KEY = 'clinicrest.guestPhone';

interface CalendarGroup {
  ymd: string;
  label: string;
  slots: SlotApiDto[];
}

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, UtcToLocalPipe],
  templateUrl: './register.page.html',
  styleUrl: './register.page.scss',
})
export class RegisterPage implements OnInit, OnDestroy {
  private static readonly SUCCESS_MESSAGE_DURATION_MS = 2500;
  private successMessageTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private updatedSlotsTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  readonly booking = inject(BookingStateService);
  selectedSlotId: string | null = null;
  selectedSlotSummary: string | null = null;
  successMessage: string | null = null;
  slotsJustUpdated = false;
  isSubmitting = false;
  isRegisterEntry = false;
  backPhoneQuery: string | null = null;

  readonly form = this.fb.nonNullable.group({
    phoneNumber: ['', [Validators.required, Validators.pattern(/^[0-9]*$/)]]
  });

  ngOnInit(): void {
    this.isRegisterEntry = this.route.snapshot.routeConfig?.path === 'register';

    const storedPhone = this.getStoredGuestPhone();
    if (storedPhone) {
      this.form.controls.phoneNumber.setValue(this.normalizePhoneDigits(storedPhone));
    }

    this.route.queryParamMap.subscribe((params) => {
      const phoneFromQuery = params.get('phone')?.trim() || null;
      this.backPhoneQuery = phoneFromQuery ? this.normalizePhoneDigits(phoneFromQuery) : null;
      if (phoneFromQuery) {
        const normalizedPhone = this.normalizePhoneDigits(phoneFromQuery);
        this.form.controls.phoneNumber.setValue(normalizedPhone);
        this.storeGuestPhone(normalizedPhone);
      }
    });

    if (!this.isRegisterEntry) {
      this.booking.loadAllSlots();
    }
  }

  ngOnDestroy(): void {
    this.clearSuccessMessageTimer();
    this.clearUpdatedSlotsTimer();
  }

  submit(): void {
    if (this.isRegisterEntry) {
      this.continueToRequest();
      return;
    }

    if (this.isSubmitting || this.form.invalid || this.booking.loading() || !this.selectedSlotId) {
      this.form.markAllAsTouched();
      return;
    }

    const { phoneNumber } = this.form.getRawValue();
    const normalizedPhone = this.normalizePhoneDigits(phoneNumber);
    this.booking.resetBookingError();
    this.successMessage = null;
    this.isSubmitting = true;

    this.booking.bookSlot(this.selectedSlotId, { phoneNumber: normalizedPhone }).subscribe({
      next: () => {
        this.booking.loadAllSlots();
        this.storeGuestPhone(normalizedPhone);
        this.selectedSlotId = null;
        this.selectedSlotSummary = null;
        this.showSuccessFeedback();
        this.isSubmitting = false;
      },
      error: () => {
        this.isSubmitting = false;
      },
    });
  }

  continueToRequest(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { phoneNumber } = this.form.getRawValue();
    const normalizedPhone = this.normalizePhoneDigits(phoneNumber);
    this.storeGuestPhone(normalizedPhone);
    void this.router.navigate(['/request'], { queryParams: { phone: normalizedPhone } });
  }

  goBack(): void {
    if (this.backPhoneQuery) {
      void this.router.navigate(['/register'], { queryParams: { phone: this.backPhoneQuery } });
      return;
    }

    if (typeof window !== 'undefined') {
      window.history.back();
    }
  }

  onPhoneInput(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    if (!input) {
      return;
    }

    const digitsOnly = this.normalizePhoneDigits(input.value);
    if (digitsOnly !== input.value) {
      input.value = digitsOnly;
    }

    this.form.controls.phoneNumber.setValue(digitsOnly, { emitEvent: false });
  }

  private normalizePhoneDigits(value: string): string {
    return value.replace(/[^0-9]/g, '');
  }

  selectSlot(slot: SlotApiDto): void {
    if (!this.isSlotSelectable(slot)) {
      return;
    }
    this.selectedSlotId = slot.id;
    this.selectedSlotSummary = `${new Date(slot.start_time).toLocaleDateString()} ${new Date(slot.start_time).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
    this.successMessage = null;
  }

  isSlotSelectable(slot: SlotApiDto): boolean {
    const startsAt = new Date(slot.start_time).getTime();
    const now = Date.now();
    return this.availableSlots(slot) > 0 && startsAt >= now;
  }

  availableSlots(slot: SlotApiDto): number {
    return Math.max(0, slot.capacity - slot.booked_count);
  }

  refreshSlots(): void {
    this.booking.loadAllSlots();
  }

  private showSuccessFeedback(): void {
    this.successMessage = 'Booking complete';
    this.slotsJustUpdated = true;
    this.clearSuccessMessageTimer();
    this.clearUpdatedSlotsTimer();

    if (typeof window !== 'undefined') {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    this.successMessageTimeoutId = setTimeout(() => {
      this.successMessage = null;
      this.successMessageTimeoutId = null;
    }, RegisterPage.SUCCESS_MESSAGE_DURATION_MS);

    this.updatedSlotsTimeoutId = setTimeout(() => {
      this.slotsJustUpdated = false;
      this.updatedSlotsTimeoutId = null;
    }, 1200);
  }

  private clearSuccessMessageTimer(): void {
    if (this.successMessageTimeoutId !== null) {
      clearTimeout(this.successMessageTimeoutId);
      this.successMessageTimeoutId = null;
    }
  }

  private clearUpdatedSlotsTimer(): void {
    if (this.updatedSlotsTimeoutId !== null) {
      clearTimeout(this.updatedSlotsTimeoutId);
      this.updatedSlotsTimeoutId = null;
    }
  }

  calendarGroups(): CalendarGroup[] {
    const grouped = new Map<string, SlotApiDto[]>();
    const slots = this.booking.slots();
    for (const slot of slots) {
      const date = new Date(slot.start_time);
      const ymd = this.toYmdUtc(date);
      const list = grouped.get(ymd);
      if (list) {
        list.push(slot);
      } else {
        grouped.set(ymd, [slot]);
      }
    }

    return Array.from(grouped.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([ymd, daySlots]) => ({
        ymd,
        label: new Date(`${ymd}T00:00:00Z`).toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric' }),
        slots: daySlots.sort((a, b) => new Date(a.start_time).getTime() - new Date(b.start_time).getTime()),
      }));
  }

  private toYmdUtc(value: Date): string {
    const y = value.getUTCFullYear();
    const m = String(value.getUTCMonth() + 1).padStart(2, '0');
    const d = String(value.getUTCDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
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
