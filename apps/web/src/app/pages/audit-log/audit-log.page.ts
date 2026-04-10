import { NgClass } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { AuditLogDto, AuditLogService, PagedAuditLogs } from '../../audit-logs/audit-log.service';

@Component({
  selector: 'app-audit-log-page',
  standalone: true,
  imports: [NgClass],
  templateUrl: './audit-log.page.html',
  styleUrl: './audit-log.page.scss'
})
export class AuditLogPage implements OnInit {
  private readonly auditLogService = inject(AuditLogService);

  readonly rows = signal<AuditLogDto[]>([]);
  readonly totalCount = signal(0);
  readonly pageNumber = signal(1);
  readonly pageSize = signal(25);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly expandedId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.auditLogService.getPaged(this.pageNumber(), this.pageSize()).subscribe({
      next: (res: PagedAuditLogs) => {
        this.rows.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: (err: { status?: number }) => {
        if (err.status === 403) {
          this.error.set('You do not have permission to view audit logs.');
        } else {
          this.error.set('Could not load audit logs.');
        }
        this.loading.set(false);
      }
    });
  }

  toggleExpand(id: string): void {
    this.expandedId.update((current) => (current === id ? null : id));
  }

  isExpanded(id: string): boolean {
    return this.expandedId() === id;
  }

  displayUser(row: AuditLogDto): string {
    return row.actorUsername ?? row.userId;
  }

  formatTimestamp(iso: string): string {
    const d = new Date(iso);
    return new Intl.DateTimeFormat(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short'
    }).format(d);
  }

  prettyJson(raw: string | null | undefined): string {
    if (!raw) {
      return '—';
    }
    try {
      const parsed: unknown = JSON.parse(raw);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return raw;
    }
  }

  totalPages(): number {
    const t = this.totalCount();
    const s = this.pageSize();
    return Math.max(1, Math.ceil(t / s));
  }

  prevPage(): void {
    if (this.pageNumber() <= 1) {
      return;
    }
    this.pageNumber.update((p) => p - 1);
    this.load();
  }

  nextPage(): void {
    if (this.pageNumber() >= this.totalPages()) {
      return;
    }
    this.pageNumber.update((p) => p + 1);
    this.load();
  }
}
