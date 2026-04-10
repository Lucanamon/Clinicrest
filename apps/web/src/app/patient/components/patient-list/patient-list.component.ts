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
import { NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { Subject, Subscription, debounceTime } from 'rxjs';
import { PatientDto, PatientService } from '../../patient.service';
import { AuthService } from '../../../services/auth';
import { toggleSort } from '../../../shared/sorting/toggle-sort';

@Component({
  selector: 'app-patient-list',
  standalone: true,
  imports: [NgFor, NgIf, RouterLink, FormsModule],
  templateUrl: './patient-list.component.html',
  styleUrl: './patient-list.component.scss'
})
export class PatientListComponent implements OnInit, OnDestroy {
  private readonly patientService = inject(PatientService);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  readonly auth = inject(AuthService);

  /** Single source of truth for the search box; survives reloads while this service lives. */
  readonly searchTerm = toSignal(this.patientService.getSearchTerm(), {
    initialValue: this.patientService.getSearchTermSnapshot()
  });

  readonly sortBy = toSignal(this.patientService.getSortBy(), {
    initialValue: this.patientService.getSortBySnapshot()
  });

  readonly sortDirection = toSignal(this.patientService.getSortDirection(), {
    initialValue: this.patientService.getSortDirectionSnapshot()
  });

  readonly patients = signal<PatientDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly genderFilter = signal('');
  readonly fromDateOfBirth = signal('');
  readonly toDateOfBirth = signal('');
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
      this.loadPatients();
    });
  }

  ngOnInit(): void {
    this.searchDebounceSub = this.searchTrigger$.pipe(debounceTime(300)).subscribe(() => {
      this.page.set(1);
      this.loadPatients();
    });
  }

  ngOnDestroy(): void {
    this.searchDebounceSub?.unsubscribe();
    this.searchTrigger$.complete();
  }

  onSearchChange(value: string): void {
    this.patientService.setSearchTerm(value);
    this.searchTrigger$.next();
  }

  onGenderChange(value: string): void {
    this.genderFilter.set(value);
    this.page.set(1);
    this.loadPatients();
  }

  onPageSizeChange(value: string): void {
    const n = parseInt(value, 10);
    if (!Number.isFinite(n) || n < 1) {
      return;
    }
    this.pageSize.set(n);
    this.page.set(1);
    this.loadPatients();
  }

  onFromDobChange(value: string): void {
    this.fromDateOfBirth.set(value);
    this.page.set(1);
    this.loadPatients();
  }

  onToDobChange(value: string): void {
    this.toDateOfBirth.set(value);
    this.page.set(1);
    this.loadPatients();
  }

  goToDetail(id: string): void {
    void this.router.navigate(['/patients', id]);
  }

  goToReport(): void {
    void this.router.navigate(['/patients/report']);
  }

  goToPage(nextPage: number): void {
    const max = this.totalPages();
    if (nextPage < 1 || (max > 0 && nextPage > max)) {
      return;
    }
    this.page.set(nextPage);
    this.loadPatients();
  }

  onSort(column: string): void {
    const next = toggleSort(
      column,
      this.patientService.getSortBySnapshot(),
      this.patientService.getSortDirectionSnapshot()
    );
    this.patientService.setSort(next.sortBy, next.sortDirection);
    this.loadPatients();
  }

  loadPatients(): void {
    this.loading.set(true);
    this.error.set(null);
    const search = this.patientService.getSearchTermSnapshot().trim();
    const gender = this.genderFilter().trim();
    const fromDob = this.fromDateOfBirth().trim();
    const toDob = this.toDateOfBirth().trim();

    this.patientService
      .getPaged({
        pageNumber: this.page(),
        pageSize: this.pageSize(),
        searchTerm: search || undefined,
        gender: gender || undefined,
        fromDateOfBirth: fromDob || undefined,
        toDateOfBirth: toDob || undefined,
        sortBy: this.patientService.getSortBySnapshot(),
        sortDirection: this.patientService.getSortDirectionSnapshot()
      })
      .subscribe({
        next: (res) => {
          this.patients.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Unable to load patients. Please try again.');
          this.loading.set(false);
        }
      });
  }

  confirmDelete(patient: PatientDto): void {
    const label = `${patient.firstName} ${patient.lastName}`.trim();
    const ok = window.confirm(
      `Remove patient "${label}" from the list? The record is kept but hidden (soft delete).`
    );
    if (!ok) {
      return;
    }

    this.patientService.delete(patient.id).subscribe({
      next: () => {
        const remainingOnPage = this.patients().length - 1;
        if (remainingOnPage === 0 && this.page() > 1) {
          this.page.update((p) => p - 1);
        }
        this.loadPatients();
      },
      error: () => this.error.set('Delete failed. Please try again.')
    });
  }

  formatDate(iso: string): string {
    const d = new Date(iso);
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(d);
  }
}
