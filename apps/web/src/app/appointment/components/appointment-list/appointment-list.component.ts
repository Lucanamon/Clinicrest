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
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { Subject, Subscription, debounceTime } from 'rxjs';
import { AppointmentDto, AppointmentService } from '../../appointment.service';
import { AuthService } from '../../../services/auth';

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

  constructor() {
    if (!isPlatformBrowser(this.platformId)) {
      this.loading.set(false);
    }
    afterNextRender(() => {
      this.loadAppointments();
    });
  }

  ngOnInit(): void {
    this.searchDebounceSub = this.searchTrigger$.pipe(debounceTime(300)).subscribe(() => {
      this.page.set(1);
      this.loadAppointments();
    });
  }

  ngOnDestroy(): void {
    this.searchDebounceSub?.unsubscribe();
    this.searchTrigger$.complete();
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
          this.appointments.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Unable to load appointments. Please try again.');
          this.loading.set(false);
        }
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

  formatDateTime(iso: string): string {
    const d = new Date(iso);
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(d);
  }

  canEdit(): boolean {
    return this.auth.isClinicalStaff();
  }
}
