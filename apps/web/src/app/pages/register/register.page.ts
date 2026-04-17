import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { startWith } from 'rxjs/operators';
import { BookingStateService } from '../../booking/booking-state.service';
import { BookingService } from '../../booking/booking.service';
import { AppointmentService } from '../../appointment/appointment.service';
import type { SlotApiDto } from '../../booking/booking-api.types';
import { UtcToLocalPipe } from '../../booking/utc-to-local.pipe';

const GUEST_PHONE_STORAGE_KEY = 'clinicrest.guestPhone';

interface DateOption {
  ymd: string;
  label: string;
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
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly bookingService = inject(BookingService);
  private readonly appointmentService = inject(AppointmentService);
  readonly booking = inject(BookingStateService);
  @ViewChild('step2Section') private step2Section?: ElementRef<HTMLElement>;
  @ViewChild('step3Section') private step3Section?: ElementRef<HTMLElement>;
  readonly selectedDateYmd = signal<string | null>(null);
  readonly selectedSlotId = signal<number | null>(null);
  successMessage: string | null = null;
  submitError: string | null = null;
  isSubmitting = false;
  isRegisterEntry = false;
  backPhoneQuery: string | null = null;

  readonly form = this.fb.nonNullable.group({
    phoneNumber: ['', [Validators.required, Validators.pattern(/^[0-9]*$/)]],
    patientName: ['', [Validators.required, Validators.maxLength(500)]],
  });
  private readonly formStatus = toSignal(this.form.statusChanges.pipe(startWith(this.form.status)), {
    initialValue: this.form.status,
  });
  readonly dateOptions = computed(() => this.buildDateOptions());
  readonly slotsByTimeOfDay = computed(() => {
    const now = Date.now();
    const morning: SlotApiDto[] = [];
    const afternoon: SlotApiDto[] = [];
    for (const slot of this.booking.slots()) {
      const start = new Date(slot.start_time);
      if (start.getTime() < now) {
        continue;
      }
      if (start.getHours() < 12) {
        morning.push(slot);
      } else {
        afternoon.push(slot);
      }
    }
    const sortByTime = (a: SlotApiDto, b: SlotApiDto) => new Date(a.start_time).getTime() - new Date(b.start_time).getTime();
    morning.sort(sortByTime);
    afternoon.sort(sortByTime);
    return { morning, afternoon };
  });
  readonly canShowStep2 = computed(() => this.selectedDateYmd() !== null);
  readonly canShowStep3 = computed(() => this.selectedSlotId() !== null);
  readonly canSubmitRequest = computed(() => {
    const hasSlot = this.selectedSlotId() !== null;
    const formReady = this.formStatus() === 'VALID';
    return hasSlot && formReady && !this.isSubmitting && this.booking.bookingSlotId() === null;
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
      const firstDate = this.dateOptions()[0];
      if (firstDate) {
        this.selectDate(firstDate.ymd, false);
      }
    }
  }

  ngOnDestroy(): void {
    this.clearSuccessMessageTimer();
  }

  submit(): void {
    if (this.isRegisterEntry) {
      this.continueToRequest();
      return;
    }

    if (this.isSubmitting) {
      return;
    }

    if (!this.canSubmitRequest()) {
      this.form.markAllAsTouched();
      if (!this.selectedSlotId()) {
        this.submitError = 'Please choose a time first.';
      }
      return;
    }

    const { patientName, phoneNumber } = this.form.getRawValue();
    const selectedSlotId = this.selectedSlotId();
    if (selectedSlotId === null) {
      return;
    }
    const name = patientName.trim();
    const normalizedPhone = this.normalizePhoneDigits(phoneNumber);
    this.storeGuestPhone(normalizedPhone);
    this.successMessage = null;
    this.submitError = null;
    this.isSubmitting = true;

    this.bookingService.createBooking(selectedSlotId, name, normalizedPhone).subscribe({
      next: () => {
        this.booking.loadSlots();
        this.appointmentService.requestRefresh();
        this.selectedSlotId.set(null);
        this.form.controls.patientName.reset('');
        this.showSuccessFeedback();
        this.isSubmitting = false;
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 409) {
          this.submitError =
            'It looks like you already have a pending request. Please wait for our staff to contact you.';
        } else {
          this.submitError = 'We could not send your request. Please try again.';
        }
        this.isSubmitting = false;
      },
    });
  }

  continueToRequest(): void {
    const phoneControl = this.form.controls.phoneNumber;
    if (phoneControl.invalid) {
      phoneControl.markAsTouched();
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
    this.selectedSlotId.set(slot.id);
    this.submitError = null;
    this.successMessage = null;
    if (typeof window !== 'undefined') {
      setTimeout(() => {
        this.step3Section?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, 80);
    }
  }

  isSlotSelectable(slot: SlotApiDto): boolean {
    const startsAt = new Date(slot.start_time).getTime();
    const now = Date.now();
    return slot.available_slots > 0 && startsAt >= now;
  }

  refreshSlots(): void {
    this.booking.loadSlots();
  }

  selectDate(ymd: string, scrollToTimes = true): void {
    this.selectedDateYmd.set(ymd);
    this.selectedSlotId.set(null);
    this.submitError = null;
    this.successMessage = null;
    this.booking.setSelectedDateYmd(ymd);
    if (scrollToTimes && typeof window !== 'undefined') {
      setTimeout(() => {
        this.step2Section?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, 80);
    }
  }

  isDateSelected(ymd: string): boolean {
    return this.selectedDateYmd() === ymd;
  }

  private showSuccessFeedback(): void {
    this.successMessage = 'Booking request sent.';
    this.clearSuccessMessageTimer();

    if (typeof window !== 'undefined') {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    this.successMessageTimeoutId = setTimeout(() => {
      this.successMessage = null;
      this.successMessageTimeoutId = null;
    }, RegisterPage.SUCCESS_MESSAGE_DURATION_MS);
  }

  private clearSuccessMessageTimer(): void {
    if (this.successMessageTimeoutId !== null) {
      clearTimeout(this.successMessageTimeoutId);
      this.successMessageTimeoutId = null;
    }
  }

  private buildDateOptions(days = 7): DateOption[] {
    const items: DateOption[] = [];
    const now = new Date();
    for (let i = 0; i < days; i += 1) {
      const d = new Date(now);
      d.setDate(now.getDate() + i);
      const ymd = this.toYmdUtc(d);
      let label = d.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
      if (i === 0) {
        label = 'Today';
      } else if (i === 1) {
        label = 'Tomorrow';
      }
      items.push({ ymd, label });
    }
    return items;
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
