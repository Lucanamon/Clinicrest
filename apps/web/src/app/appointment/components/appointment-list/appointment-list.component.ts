import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { NgFor } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { EMPTY, Subject, Subscription, debounceTime, expand, map, reduce } from 'rxjs';
import { PatientDto, PatientService } from '../../../patient/patient.service';
import { BookingApiDto } from '../../../booking/booking-api.types';
import { BookingService } from '../../../booking/booking.service';

@Component({
  selector: 'app-appointment-list',
  standalone: true,
  imports: [NgFor, FormsModule],
  templateUrl: './appointment-list.component.html',
  styleUrl: './appointment-list.component.scss'
})
export class AppointmentListComponent implements OnInit, OnDestroy {
  private readonly bookingService = inject(BookingService);
  private readonly router = inject(Router);
  private readonly patientService = inject(PatientService);

  readonly triageRows = signal<BookingApiDto[]>([]);
  readonly allTriageRows = signal<BookingApiDto[]>([]);
  readonly searchTerm = signal('');
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly registeredPatientMap = signal<Record<string, PatientDto>>({});

  private readonly searchTrigger$ = new Subject<void>();
  private searchDebounceSub?: Subscription;
  private successTimeoutId: ReturnType<typeof setTimeout> | null = null;

  constructor() {}

  ngOnInit(): void {
    const success = this.router.getCurrentNavigation()?.extras.state?.['successMessage'] as string | undefined;
    if (success) {
      this.successMessage.set(success);
      if (this.successTimeoutId !== null) {
        clearTimeout(this.successTimeoutId);
      }
      this.successTimeoutId = setTimeout(() => {
        this.successMessage.set(null);
        this.successTimeoutId = null;
      }, 3500);
    }

    this.searchDebounceSub = this.searchTrigger$.pipe(debounceTime(300)).subscribe(() => {
      this.applyClientFilter();
    });
    this.loadTriageInbox();
  }

  ngOnDestroy(): void {
    this.searchDebounceSub?.unsubscribe();
    this.searchTrigger$.complete();
    if (this.successTimeoutId !== null) {
      clearTimeout(this.successTimeoutId);
      this.successTimeoutId = null;
    }
  }

  onSearchChange(value: string): void {
    this.searchTerm.set(value);
    this.searchTrigger$.next();
  }

  loadTriageInbox(): void {
    this.loading.set(true);
    this.error.set(null);

    this.bookingService
      .getBookings('ACTIVE')
      .subscribe({
        next: (rows) => {
          this.updateRegisteredPatientMap();
          this.allTriageRows.set(rows);
          this.applyClientFilter();
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Unable to load pending requests. Please try again.');
          this.loading.set(false);
        }
      });
  }

  onSendToSchedule(row: BookingApiDto): void {
    const matched = this.getRegisteredPatient(row);
    if (!matched) {
      this.createProfile(row);
      return;
    }

    const bookingId = row.id;
    this.bookingService.scheduleBooking(bookingId, matched.id).subscribe({
      next: () => {
        this.successMessage.set('Request moved to Appointments.');
        this.loadTriageInbox();
      },
      error: () => {
        this.error.set('Could not move request to the appointments list.');
      }
    });
  }

  formatDateTime(iso?: string | null): string {
    if (!iso) {
      return 'N/A';
    }
    const d = new Date(iso);
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(d);
  }

  isRegisteredBooking(row: BookingApiDto): boolean {
    return this.getRegisteredPatient(row) != null;
  }

  getProcessLabel(row: BookingApiDto): string {
    return this.isRegisteredBooking(row) ? 'Send to Appointments' : 'Create Profile';
  }

  createProfile(row: BookingApiDto): void {
    const [firstName, ...lastNameParts] = row.patient_name.trim().split(/\s+/);
    const lastName = lastNameParts.join(' ');
    const patientName = row.patient_name.trim();
    const phoneNumber = (row.phone_number ?? '').trim();

    void this.router.navigate(['/patients/new'], {
      queryParams: {
        patient_name: patientName,
        phone_number: phoneNumber
      },
      state: {
        patient_name: patientName,
        phone_number: phoneNumber,
        prefillPatient: {
          firstName: firstName ?? '',
          lastName,
          phoneNumber
        }
      }
    });
  }

  private updateRegisteredPatientMap(): void {
    this.patientService
      .getPaged({ pageNumber: 1, pageSize: 100, sortBy: 'createdAt', sortDirection: 'desc' })
      .pipe(
        expand((page) => {
          const loaded = page.pageNumber * page.pageSize;
          if (loaded >= page.totalCount) {
            return EMPTY;
          }
          return this.patientService.getPaged({
            pageNumber: page.pageNumber + 1,
            pageSize: page.pageSize,
            sortBy: 'createdAt',
            sortDirection: 'desc'
          });
        }),
        map((page) => page.items),
        reduce((all, items) => all.concat(items), [] as PatientDto[])
      )
      .subscribe({
        next: (patients) => {
          const mapByKey: Record<string, PatientDto> = {};
          for (const patient of patients) {
            const name = this.normalizeName(`${patient.firstName} ${patient.lastName}`);
            const phone = this.normalizePhone(patient.phoneNumber);
            if (name) {
              mapByKey[`name:${name}`] = patient;
              if (phone) {
                mapByKey[`name:${name}|phone:${phone}`] = patient;
              }
            }
          }
          this.registeredPatientMap.set(mapByKey);
        }
      });
  }

  private getRegisteredPatient(row: BookingApiDto): PatientDto | null {
    const normalizedName = this.normalizeName(row.patient_name);
    if (!normalizedName) {
      return null;
    }

    const normalizedPhone = this.normalizePhone(row.phone_number ?? null);
    const byNameAndPhone = normalizedPhone
      ? this.registeredPatientMap()[`name:${normalizedName}|phone:${normalizedPhone}`]
      : null;
    if (byNameAndPhone) {
      return byNameAndPhone;
    }

    return this.registeredPatientMap()[`name:${normalizedName}`] ?? null;
  }

  private normalizeName(value: string): string {
    return value.trim().toLowerCase().replace(/\s+/g, ' ');
  }

  private normalizePhone(value: string | null): string {
    return (value ?? '').replace(/\D+/g, '');
  }

  private applyClientFilter(): void {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) {
      this.triageRows.set(this.allTriageRows());
      return;
    }
    const filtered = this.allTriageRows().filter((row) => row.patient_name.toLowerCase().includes(term));
    this.triageRows.set(filtered);
  }
}
