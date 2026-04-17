import { isPlatformBrowser } from '@angular/common';
import { Component, HostListener, OnInit, PLATFORM_ID, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AppointmentService } from '../../appointment/appointment.service';
import { AuthService } from '../../services/auth';
import { DoctorListItemDto, UsersService } from '../../users/users.service';

type ScheduleNavigationState = {
  bookingId?: number;
  patientName?: string;
  phoneNumber?: string | null;
  appointmentDate?: string;
  patientId?: string | null;
};

@Component({
  selector: 'app-schedule-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './schedule.page.html',
  styleUrl: './schedule.page.scss'
})
export class SchedulePage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly usersService = inject(UsersService);
  private readonly appointmentService = inject(AppointmentService);
  private readonly platformId = inject(PLATFORM_ID);
  readonly auth = inject(AuthService);

  readonly loadingDoctors = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly patientName = signal('');
  readonly phoneNumber = signal('');
  readonly bookingId = signal<number | null>(null);
  readonly patientId = signal<string | null>(null);
  readonly doctors = signal<DoctorListItemDto[]>([]);
  readonly hasSaved = signal(false);

  readonly form = this.fb.nonNullable.group({
    doctorId: ['', Validators.required],
    appointmentDate: ['', Validators.required],
    notes: ['', [Validators.maxLength(2000)]]
  });

  ngOnInit(): void {
    const nav = this.router.getCurrentNavigation();
    const browserState = isPlatformBrowser(this.platformId) ? (history.state as ScheduleNavigationState) : {};
    const state = (nav?.extras.state ?? browserState) as ScheduleNavigationState;

    const bookingId = state.bookingId;
    if (!bookingId) {
      void this.router.navigateByUrl('/appointments');
      return;
    }

    this.bookingId.set(bookingId);
    this.patientName.set((state.patientName ?? '').trim());
    this.phoneNumber.set((state.phoneNumber ?? '').trim());
    this.patientId.set(state.patientId?.trim() || null);

    const appointmentDate = state.appointmentDate ? this.toDatetimeLocal(state.appointmentDate) : '';
    this.form.patchValue({
      appointmentDate
    });

    this.loadDoctors();
  }

  private loadDoctors(): void {
    this.loadingDoctors.set(true);
    this.usersService.getDoctors().subscribe({
      next: (rows) => {
        this.doctors.set(rows);
        this.loadingDoctors.set(false);
      },
      error: () => {
        this.error.set('Could not load doctors.');
        this.loadingDoctors.set(false);
      }
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const bookingId = this.bookingId();
    const patientId = this.patientId();
    if (!bookingId || !patientId) {
      this.error.set('This booking is missing patient linkage. Please create or select a patient profile first.');
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);

    this.appointmentService
      .finalize({
        booking_id: bookingId,
        patient_id: patientId,
        doctor_id: value.doctorId,
        appointment_date: new Date(value.appointmentDate).toISOString(),
        notes: value.notes.trim() ? value.notes.trim() : null
      })
      .subscribe({
        next: () => {
          this.hasSaved.set(true);
          this.saving.set(false);
          void this.router.navigate(['/appointments'], {
            state: { successMessage: 'Appointment finalized successfully.' }
          });
        },
        error: (err) => {
          const apiMessage = err?.error?.message;
          this.error.set(typeof apiMessage === 'string' && apiMessage ? apiMessage : 'Could not finalize appointment.');
          this.saving.set(false);
        }
      });
  }

  hasUnsavedChanges(): boolean {
    return !this.hasSaved() && this.form.dirty;
  }

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (!this.hasUnsavedChanges()) {
      return;
    }
    event.preventDefault();
    event.returnValue = '';
  }

  private toDatetimeLocal(iso: string): string {
    const d = new Date(iso);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }
}
