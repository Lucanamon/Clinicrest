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
import { NgClass, NgFor } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { Subject, Subscription, debounceTime } from 'rxjs';
import { BacklogDto, BacklogService } from '../../backlog.service';
import { AuthService } from '../../../services/auth';

@Component({
  selector: 'app-backlog-list',
  standalone: true,
  imports: [NgClass, NgFor, RouterLink, FormsModule],
  templateUrl: './backlog-list.component.html',
  styleUrl: './backlog-list.component.scss'
})
export class BacklogListComponent implements OnInit, OnDestroy {
  private readonly backlogService = inject(BacklogService);
  private readonly platformId = inject(PLATFORM_ID);
  readonly auth = inject(AuthService);

  readonly searchTerm = toSignal(this.backlogService.getSearchTerm(), {
    initialValue: this.backlogService.getSearchTermSnapshot()
  });

  readonly sortBy = toSignal(this.backlogService.getSortBy(), {
    initialValue: this.backlogService.getSortBySnapshot()
  });

  readonly sortDirection = toSignal(this.backlogService.getSortDirection(), {
    initialValue: this.backlogService.getSortDirectionSnapshot()
  });

  readonly items = signal<BacklogDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly statusFilter = signal('');
  readonly priorityFilter = signal('');
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
      this.loadBacklogs();
    });
  }

  ngOnInit(): void {
    this.searchDebounceSub = this.searchTrigger$.pipe(debounceTime(300)).subscribe(() => {
      this.page.set(1);
      this.loadBacklogs();
    });
  }

  ngOnDestroy(): void {
    this.searchDebounceSub?.unsubscribe();
    this.searchTrigger$.complete();
  }

  onSearchChange(value: string): void {
    this.backlogService.setSearchTerm(value);
    this.searchTrigger$.next();
  }

  onStatusChange(value: string): void {
    this.statusFilter.set(value);
    this.page.set(1);
    this.loadBacklogs();
  }

  onPriorityChange(value: string): void {
    this.priorityFilter.set(value);
    this.page.set(1);
    this.loadBacklogs();
  }

  onPageSizeChange(value: string): void {
    const n = parseInt(value, 10);
    if (!Number.isFinite(n) || n < 1) {
      return;
    }
    this.pageSize.set(n);
    this.page.set(1);
    this.loadBacklogs();
  }

  goToPage(nextPage: number): void {
    const max = this.totalPages();
    if (nextPage < 1 || (max > 0 && nextPage > max)) {
      return;
    }
    this.page.set(nextPage);
    this.loadBacklogs();
  }

  onSortColumn(column: string): void {
    const current = this.backlogService.getSortBySnapshot();
    const dir = this.backlogService.getSortDirectionSnapshot();
    if (current === column) {
      this.backlogService.setSort(column, dir === 'asc' ? 'desc' : 'asc');
    } else {
      this.backlogService.setSort(column, 'asc');
    }
    this.loadBacklogs();
  }

  loadBacklogs(): void {
    this.loading.set(true);
    this.error.set(null);
    const search = this.backlogService.getSearchTermSnapshot().trim();
    const status = this.statusFilter().trim();
    const priority = this.priorityFilter().trim();

    this.backlogService
      .getPaged({
        pageNumber: this.page(),
        pageSize: this.pageSize(),
        searchTerm: search || undefined,
        status: status || undefined,
        priority: priority || undefined,
        sortBy: this.backlogService.getSortBySnapshot(),
        sortDirection: this.backlogService.getSortDirectionSnapshot()
      })
      .subscribe({
        next: (res) => {
          this.items.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Unable to load backlog. Please try again.');
          this.loading.set(false);
        }
      });
  }

  confirmDelete(row: BacklogDto): void {
    const ok = window.confirm(`Delete task "${row.title}"?`);
    if (!ok) {
      return;
    }

    this.backlogService.delete(row.id).subscribe({
      next: () => {
        const remainingOnPage = this.items().length - 1;
        if (remainingOnPage === 0 && this.page() > 1) {
          this.page.update((p) => p - 1);
        }
        this.loadBacklogs();
      },
      error: () => this.error.set('Delete failed. Please try again.')
    });
  }

  formatDate(iso: string): string {
    const d = new Date(iso);
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(d);
  }

  priorityClass(priority: string): string {
    switch (priority) {
      case 'High':
        return 'priority-high';
      case 'Medium':
        return 'priority-medium';
      case 'Low':
        return 'priority-low';
      default:
        return 'priority-default';
    }
  }

  canEdit(row: BacklogDto): boolean {
    if (this.auth.isAdmin()) {
      return true;
    }
    return this.auth.isDoctor() && this.auth.getUserId() === row.assignedToUserId;
  }
}
