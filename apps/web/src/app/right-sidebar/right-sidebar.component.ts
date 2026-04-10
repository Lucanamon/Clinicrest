import { CommonModule } from '@angular/common';
import { isPlatformBrowser } from '@angular/common';
import { Component, Inject, OnDestroy, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, interval, of } from 'rxjs';
import { catchError, startWith, switchMap, takeUntil } from 'rxjs/operators';
import { UserDto, UsersService } from '../users/users.service';

@Component({
  selector: 'app-right-sidebar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './right-sidebar.component.html',
  styleUrl: './right-sidebar.component.scss'
})
export class RightSidebarComponent implements OnInit, OnDestroy {
  private readonly usersService = inject(UsersService);
  private readonly router = inject(Router);
  private readonly destroy$ = new Subject<void>();

  readonly defaultAvatar = 'https://ui-avatars.com/api/?name=User';
  sortedUsers: UserDto[] = [];

  private readonly rolePriority: Record<string, number> = {
    rootadmin: 1,
    administrator: 2,
    doctor: 3,
    nurse: 4
  };

  constructor(@Inject(PLATFORM_ID) private readonly platformId: object) {}

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    interval(10000)
      .pipe(
        startWith(0),
        switchMap(() => this.usersService.getUsers().pipe(catchError(() => of([])))),
        takeUntil(this.destroy$)
      )
      .subscribe((users) => {
        this.sortedUsers = [...users].sort((a, b) => this.getRolePriority(a.role) - this.getRolePriority(b.role));
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getLastActive(date: string): string {
    const diff = (Date.now() - new Date(date).getTime()) / 1000;

    if (diff < 60) return 'Active now';
    if (diff < 3600) return `${Math.floor(diff / 60)} min ago`;
    if (diff < 86400) return `${Math.floor(diff / 3600)} hr ago`;

    return new Date(date).toLocaleDateString();
  }

  getRoleLabel(role: string): string {
    const normalized = role.trim().toLowerCase();
    if (normalized === 'rootadmin') return 'RootAdmin';
    if (normalized === 'administrator') return 'Administrator';
    if (normalized === 'doctor') return 'Doctor';
    if (normalized === 'nurse') return 'Nurse';
    return role;
  }

  getRoleClass(role: string): string {
    const normalized = role.trim().toLowerCase();
    return `role-${normalized}`;
  }

  isActiveNow(date: string): boolean {
    return Date.now() - new Date(date).getTime() < 60_000;
  }

  openProfile(): void {
    void this.router.navigateByUrl('/profile');
  }

  onImageError(event: Event): void {
    const target = event.target as HTMLImageElement | null;
    if (target) {
      target.src = this.defaultAvatar;
    }
  }

  private getRolePriority(role: string): number {
    return this.rolePriority[role.toLowerCase()] ?? 999;
  }
}
