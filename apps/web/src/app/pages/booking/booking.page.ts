import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { BookingStateService } from '../../booking/booking-state.service';
import type { SlotApiDto } from '../../booking/booking-api.types';
import { UtcToLocalPipe } from '../../booking/utc-to-local.pipe';

interface CalendarDay {
  date: Date;
  ymd: string;
  label: string;
}

interface CalendarTime {
  value: string;
  label: string;
}

@Component({
  selector: 'app-booking-page',
  standalone: true,
  imports: [CommonModule, FormsModule, UtcToLocalPipe],
  templateUrl: './booking.page.html',
  styleUrl: './booking.page.scss',
})
export class BookingPage implements OnInit {
  readonly booking = inject(BookingStateService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly timeRows: CalendarTime[] = this.buildTimeRows();
  calendarDays: CalendarDay[] = [];
  weekStartYmd = '';
  activeDayYmd = '';
  selectedTime = '09:00';
  selectedSlot: SlotApiDto | null = null;
  doctorFilter = '';
  startTimeLocal = '';
  endTimeLocal = '';
  capacity = 1;
  localFormError: string | null = null;
  guestPhone: string | null = null;

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      this.guestPhone = params.get('phone')?.trim() || null;
    });
    this.calendarDays = this.buildWeek(new Date());
    this.weekStartYmd = this.calendarDays[0]?.ymd ?? this.toYmd(new Date());
    this.activeDayYmd = this.calendarDays[0]?.ymd ?? this.toYmd(new Date());
    this.prefillSelectedDayTimes();
    this.booking.loadAllSlots();
  }

  refresh(): void {
    this.booking.loadAllSlots();
  }

  selectDay(ymd: string): void {
    this.activeDayYmd = ymd;
    this.prefillSelectedDayTimes();
  }

  onWeekStartChange(value: string): void {
    if (!value) {
      return;
    }
    const nextWeek = this.buildWeek(new Date(`${value}T00:00:00`));
    this.calendarDays = nextWeek;
    this.weekStartYmd = nextWeek[0]?.ymd ?? value;
    this.activeDayYmd = this.weekStartYmd;
    this.selectedTime = this.timeRows[0]?.value ?? '09:00';
    this.selectedSlot = null;
    this.prefillSelectedDayTimes();
  }

  onAddSlotClick(): void {
    this.localFormError = null;
    this.booking.resetSlotCreateError();
    this.startTimeLocal = `${this.activeDayYmd}T${this.selectedTime}`;
    this.endTimeLocal = this.addMinutesToLocalDateTime(this.startTimeLocal, 30);
    this.createSlot();
  }

  slotAt(dayYmd: string, time: string): SlotApiDto | null {
    const key = `${dayYmd}-${time}`;
    return (
      this.booking.slots().find((slot) => {
        const start = new Date(slot.start_time);
        return `${this.toYmd(start)}-${this.toHm(start)}` === key;
      }) ?? null
    );
  }

  openCell(dayYmd: string, time: string): void {
    this.activeDayYmd = dayYmd;
    this.selectedTime = time;
    const existing = this.slotAt(dayYmd, time);
    if (existing) {
      this.selectedSlot = existing;
      this.startTimeLocal = this.toLocalInput(existing.start_time);
      this.endTimeLocal = this.toLocalInput(existing.end_time);
      this.capacity = existing.capacity;
      return;
    }

    this.selectedSlot = null;
    this.localFormError = null;
    this.booking.resetSlotCreateError();
    this.startTimeLocal = `${dayYmd}T${time}`;
    this.endTimeLocal = this.addMinutesToLocalDateTime(this.startTimeLocal, 30);
    this.createSlot();
  }

  slotsForDay(ymd: string): SlotApiDto[] {
    return this.booking
      .slots()
      .filter((slot) => this.toYmd(new Date(slot.start_time)) === ymd)
      .sort((a, b) => new Date(a.start_time).getTime() - new Date(b.start_time).getTime());
  }

  createSlot(): void {
    this.selectedSlot = null;
    this.localFormError = null;
    this.booking.resetSlotCreateError();
    const start = this.parseLocalDateTime(this.startTimeLocal);
    const end = this.parseLocalDateTime(this.endTimeLocal);

    if (!start || !end) {
      this.localFormError = 'Start time and end time are required.';
      return;
    }

    if (end <= start) {
      this.localFormError = 'End time must be after start time.';
      return;
    }

    this.booking
      .createSlot({
        start_time: start.toISOString(),
        end_time: end.toISOString(),
        capacity: this.capacity,
      })
      .subscribe({
        next: () => {
          this.prefillSelectedDayTimes();
          this.capacity = 1;
          this.selectedSlot = null;
        },
        error: () => undefined,
      });
  }

  backToRegister(): void {
    void this.router.navigate(['/register'], {
      queryParams: this.guestPhone ? { phone: this.guestPhone } : undefined,
    });
  }

  increaseCapacity(slot: SlotApiDto): void {
    this.booking.updateSlotCapacity(slot.id, 'increase').subscribe({
      next: (updated) => {
        if (this.selectedSlot?.id === updated.id) {
          this.selectedSlot = updated;
        }
      },
      error: () => undefined,
    });
  }

  decreaseCapacity(slot: SlotApiDto): void {
    if (slot.capacity <= slot.booked_count) {
      return;
    }

    this.booking.updateSlotCapacity(slot.id, 'decrease').subscribe({
      next: (updated) => {
        if (this.selectedSlot?.id === updated.id) {
          this.selectedSlot = updated;
        }
      },
      error: () => undefined,
    });
  }

  canDecreaseCapacity(slot: SlotApiDto): boolean {
    return slot.capacity > slot.booked_count;
  }

  private buildWeek(reference: Date): CalendarDay[] {
    const utcMidnight = new Date(Date.UTC(reference.getUTCFullYear(), reference.getUTCMonth(), reference.getUTCDate()));
    const dayIndex = utcMidnight.getUTCDay();
    const mondayDelta = dayIndex === 0 ? -6 : 1 - dayIndex;
    const monday = new Date(utcMidnight);
    monday.setUTCDate(monday.getUTCDate() + mondayDelta);

    return Array.from({ length: 7 }, (_, offset) => {
      const current = new Date(monday);
      current.setUTCDate(monday.getUTCDate() + offset);
      return {
        date: current,
        ymd: this.toYmd(current),
        label: current.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' }),
      };
    });
  }

  private buildTimeRows(): CalendarTime[] {
    const rows: CalendarTime[] = [];
    for (let hour = 9; hour < 18; hour += 1) {
      for (const minute of [0, 30]) {
        const value = `${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}`;
        rows.push({ value, label: value });
      }
    }
    return rows;
  }

  private prefillSelectedDayTimes(): void {
    const start = `${this.activeDayYmd}T${this.selectedTime}`;
    const end = this.addMinutesToLocalDateTime(start, 30);
    this.startTimeLocal = start;
    this.endTimeLocal = end;
  }

  private parseLocalDateTime(input: string): Date | null {
    if (!input || !input.includes('T')) {
      return null;
    }
    const parsed = new Date(input);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }

  private toYmd(value: Date): string {
    const y = value.getUTCFullYear();
    const m = String(value.getUTCMonth() + 1).padStart(2, '0');
    const d = String(value.getUTCDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  private toHm(value: Date): string {
    const h = String(value.getHours()).padStart(2, '0');
    const m = String(value.getMinutes()).padStart(2, '0');
    return `${h}:${m}`;
  }

  private toLocalInput(iso: string): string {
    const date = new Date(iso);
    const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    return local.toISOString().slice(0, 16);
  }

  private addMinutesToLocalDateTime(input: string, minutes: number): string {
    const parsed = this.parseLocalDateTime(input);
    if (!parsed) {
      return input;
    }
    parsed.setMinutes(parsed.getMinutes() + minutes);
    const local = new Date(parsed.getTime() - parsed.getTimezoneOffset() * 60000);
    return local.toISOString().slice(0, 16);
  }
}
