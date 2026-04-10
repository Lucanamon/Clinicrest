import { NgFor } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UserDto, UsersService } from '../../users.service';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [NgFor, RouterLink],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss'
})
export class UserListComponent implements OnInit {
  private readonly usersService = inject(UsersService);
  readonly defaultAvatar = 'https://ui-avatars.com/api/?name=User';

  readonly users = signal<UserDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.usersService.getUsers().subscribe({
      next: (rows) => {
        this.users.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load users.');
        this.loading.set(false);
      }
    });
  }

  formatDate(iso: string): string {
    const d = new Date(iso);
    return new Intl.DateTimeFormat(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short'
    }).format(d);
  }

  confirmDelete(row: UserDto): void {
    if (row.role === 'RootAdmin') {
      return;
    }
    const ok = confirm('Are you sure you want to delete this user?');
    if (!ok) {
      return;
    }
    this.usersService.deleteUser(row.id).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Delete failed. Try again.')
    });
  }

  canDelete(row: UserDto): boolean {
    return row.role !== 'RootAdmin';
  }

  onImageError(event: Event): void {
    const target = event.target as HTMLImageElement | null;
    if (target) {
      target.src = this.defaultAvatar;
    }
  }
}
