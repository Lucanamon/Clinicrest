import { isPlatformBrowser } from '@angular/common';
import {
  Component,
  OnDestroy,
  OnInit,
  PLATFORM_ID,
  afterNextRender,
  computed,
  inject,
  signal
} from '@angular/core';
import { NgFor } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { EMPTY, Subject, Subscription, debounceTime, expand, map, reduce } from 'rxjs';
import { AppointmentDto, AppointmentService } from '../../appointment.service';
import { AuthService } from '../../../services/auth';
import { PatientDto, PatientService } from '../../../patient/patient.service';

@Component({
  selector: 'app-appointment-list',
  standalone: true,
  imports: [NgFor, RouterLink, FormsModule],
  templateUrl: './appointment-list.component.html',
  styleUrl: './appointment-list.component.scss'
})
export class AppointmentListComponent implements OnInit, OnDestroy {
  private readonly appointmentService = inject(AppointmentService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly router = inject(Router);
  private readonly patientService = inject(PatientService);
  readonly auth = inject(AuthService);

  readonly searchTerm = toSignal(this.appointmentService.getSearchTerm(), {
    initialValue: this.appointmentService.getSearchTermSnapshot()
  });

  readonly sortBy = toSignal(this.appointmentService.getSortBy(), {
    initialValue: this.appointmentService.getSortBySnapshot()
  });

  readonly sortDirection = toSignal(this.appointmentService.getSortDirection(), {
    initialValue: this.appointmentService.getSortDirectionSnapshot()
  });

  readonly appointments = signal<AppointmentDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly statusFilter = signal('');
  readonly fromDate = signal('');
  readonly toDate = signal('');
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly registeredPatientMap = signal<Record<string, PatientDto>>({});

  readonly totalPages = computed(() => {
    const total = this.totalCount();
    const size = this.pageSize();
    if (size < 1 || total === 0) {
      return 0;
    }
    return Math.ceil(total / size);
  });

  private readonly searchTrigger$ = new Subject<void>();
  private searchDebounceSub?: Subscription;
  private refreshSub?: Subscription;
  private successTimeoutId: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    if (!isPlatformBrowser(this.platformId)) {
      this.loading.set(false);
    }
    afterNextRender(() => {
      this.loadAppointments();
    });
  }

  ngOnInit(): void {
    const successFromNavigation = this.router.getCurrentNavigation()?.extras.state?.['successMessage'] as string | undefined;
    const successFromHistory =
      isPlatformBrowser(this.platformId) &&
      !successFromNavigation &&
      typeof history.state?.successMessage === 'string'
        ? (history.state.successMessage as string)
        : undefined;
    const success = successFromNavigation ?? successFromHistory;
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
      this.page.set(1);
      this.loadAppointments();
    });
    this.refreshSub = this.appointmentService.getRefreshStream().subscribe(() => {
      this.page.set(1);
      this.loadAppointments();
    });
  }

  ngOnDestroy(): void {
    this.searchDebounceSub?.unsubscribe();
    this.refreshSub?.unsubscribe();
    this.searchTrigger$.complete();
    if (this.successTimeoutId !== null) {
      clearTimeout(this.successTimeoutId);
      this.successTimeoutId = null;
    }
  }

  onSearchChange(value: string): void {
    this.appointmentService.setSearchTerm(value);
    this.searchTrigger$.next();
  }

  onStatusChange(value: string): void {
    this.statusFilter.set(value);
    this.page.set(1);
    this.loadAppointments();
  }

  onPageSizeChange(value: string): void {
    const n = parseInt(value, 10);
    if (!Number.isFinite(n) || n < 1) {
      return;
    }
    this.pageSize.set(n);
    this.page.set(1);
    this.loadAppointments();
  }

  onFromDateChange(value: string): void {
    this.fromDate.set(value);
    this.page.set(1);
    this.loadAppointments();
  }

  onToDateChange(value: string): void {
    this.toDate.set(value);
    this.page.set(1);
    this.loadAppointments();
  }

  goToPage(nextPage: number): void {
    const max = this.totalPages();
    if (nextPage < 1 || (max > 0 && nextPage > max)) {
      return;
    }
    this.page.set(nextPage);
    this.loadAppointments();
  }

  onSortColumn(column: string): void {
    const current = this.appointmentService.getSortBySnapshot();
    const dir = this.appointmentService.getSortDirectionSnapshot();
    if (current === column) {
      this.appointmentService.setSort(column, dir === 'asc' ? 'desc' : 'asc');
    } else {
      this.appointmentService.setSort(column, 'asc');
    }
    this.loadAppointments();
  }

  loadAppointments(): void {
    this.loading.set(true);
    this.error.set(null);
    const search = this.appointmentService.getSearchTermSnapshot().trim();
    const status = this.statusFilter().trim();
    const from = this.fromDate().trim();
    const to = this.toDate().trim();

    this.appointmentService
      .getPaged({
        pageNumber: this.page(),
        pageSize: this.pageSize(),
        searchTerm: search || undefined,
        status: status || undefined,
        fromAppointmentDate: from || undefined,
        toAppointmentDate: to || undefined,
        sortBy: this.appointmentService.getSortBySnapshot(),
        sortDirection: this.appointmentService.getSortDirectionSnapshot()
      })
      .subscribe({
        next: (res) => {
          this.updateRegisteredPatientMap();
          this.appointments.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Error:', err);
          if (err?.status === 500) {
            console.error('Appointments API returned 500. Full error response:', {
              status: err.status,
              statusText: err.statusText,
              url: err.url,
              message: err.message,
              error: err.error,
              raw: err
            });
          }
          const errorPayload = typeof err?.error === 'string' ? err.error.trim().toLowerCase() : '';
          if (errorPayload.startsWith('<!doctype html') || errorPayload.startsWith('<html')) {
            console.error('Appointments endpoint returned HTML instead of JSON. Check API route/base URL.');
          }
          console.error('Failed to load appointments list', err);
          this.error.set('Unable to load appointments. Please try again.');
          this.loading.set(false);
        }
      });
  }

  confirmCancelBooking(row: AppointmentDto): void {
    if (!this.isBookingRow(row) || row.bookingId == null) {
      return;
    }
    const ok = window.confirm('Are you sure you want to cancel this request?');
    if (!ok) {
      return;
    }

    this.appointmentService.deleteBooking(row.bookingId).subscribe({
      next: () => {
        const remainingOnPage = this.appointments().length - 1;
        if (remainingOnPage === 0 && this.page() > 1) {
          this.page.update((p) => p - 1);
        }
        this.loadAppointments();
      },
      error: () => this.error.set('Delete failed. Please try again.')
    });
  }

  confirmDelete(row: AppointmentDto): void {
    const ok = window.confirm(
      `Remove appointment for "${row.patientName}" on ${this.formatDateTime(row.appointmentDate)}?`
    );
    if (!ok) {
      return;
    }

    this.appointmentService.delete(row.id).subscribe({
      next: () => {
        const remainingOnPage = this.appointments().length - 1;
        if (remainingOnPage === 0 && this.page() > 1) {
          this.page.update((p) => p - 1);
        }
        this.loadAppointments();
      },
      error: () => this.error.set('Delete failed. Please try again.')
    });
  }

  onProcessBooking(row: AppointmentDto): void {
    if (!this.isBookingRow(row)) {
      return;
    }

    const matched = this.getRegisteredPatient(row);
    if (!matched) {
      this.createProfile(row);
      return;
    }

    if (row.bookingId == null) {
      return;
    }

    void this.router.navigate(['/schedule'], {
      state: {
        bookingId: row.bookingId,
        patientName: row.patientName,
        phoneNumber: row.phoneNumber ?? '',
        appointmentDate: row.appointmentDate,
        patientId: matched.id
      }
    });
  }

  formatDateTime(iso: string): string {
    const d = new Date(iso);
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(d);
  }

  canEdit(): boolean {
    return this.auth.isClinicalStaff();
  }

  canEditRow(row: AppointmentDto): boolean {
    return this.canEdit() && (row.source ?? 'appointments') === 'appointments';
  }

  isBookingRow(row: AppointmentDto): boolean {
    return row.source === 'bookings';
  }

  isRegisteredBooking(row: AppointmentDto): boolean {
    return this.getRegisteredPatient(row) != null;
  }

  getProcessLabel(row: AppointmentDto): string {
    return this.isRegisteredBooking(row) ? 'Add to Schedule' : 'Create Profile';
  }

  createProfile(row: AppointmentDto): void {
    const [firstName, ...lastNameParts] = row.patientName.trim().split(/\s+/);
    const lastName = lastNameParts.join(' ');
    const patientName = row.patientName.trim();
    const phoneNumber = (row.phoneNumber ?? '').trim();

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

  private getRegisteredPatient(row: AppointmentDto): PatientDto | null {
    if (!this.isBookingRow(row)) {
      return null;
    }

    const normalizedName = this.normalizeName(row.patientName);
    if (!normalizedName) {
      return null;
    }

    const normalizedPhone = this.normalizePhone(row.phoneNumber ?? null);
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
}
