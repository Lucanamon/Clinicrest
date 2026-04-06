import { isPlatformBrowser } from '@angular/common';
import { Component, OnInit, PLATFORM_ID, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BacklogService } from '../../backlog.service';
import { AuthService } from '../../../services/auth';
import { UserDto, UsersService } from '../../../users/users.service';

@Component({
  selector: 'app-backlog-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './backlog-form.component.html',
  styleUrl: './backlog-form.component.scss'
})
export class BacklogFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly backlogService = inject(BacklogService);
  private readonly usersService = inject(UsersService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  readonly auth = inject(AuthService);

  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly users = signal<UserDto[]>([]);
  readonly lookupsLoading = signal(true);
  readonly assignedLabel = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(300)]],
    description: ['', [Validators.maxLength(4000)]],
    priority: ['Medium', [Validators.required, Validators.maxLength(16)]],
    status: ['Open', [Validators.required, Validators.maxLength(16)]],
    assignedToUserId: ['', Validators.required]
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit.set(true);
    }
    if (!isPlatformBrowser(this.platformId)) {
      this.lookupsLoading.set(false);
      return;
    }
    if (id) {
      this.loadBacklog(id);
    } else if (this.auth.isRootAdmin()) {
      this.loadUsers();
    } else {
      const uid = this.auth.getUserId();
      if (uid) {
        this.form.patchValue({ assignedToUserId: uid });
      }
      this.lookupsLoading.set(false);
    }
  }

  private loadUsers(): void {
    this.usersService.getUsers().subscribe({
      next: (u) => {
        this.users.set(u);
        this.lookupsLoading.set(false);
      },
      error: () => {
        this.users.set([]);
        this.lookupsLoading.set(false);
      }
    });
  }

  private loadBacklog(id: string): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.backlogService.getById(id).subscribe({
      next: (b) => {
        this.assignedLabel.set(b.assignedToName);
        this.form.patchValue({
          title: b.title,
          description: b.description ?? '',
          priority: b.priority,
          status: b.status,
          assignedToUserId: b.assignedToUserId
        });
        if (this.auth.isRootAdmin()) {
          this.loadUsersWhileEditing();
        } else {
          this.form.controls.assignedToUserId.disable();
          this.lookupsLoading.set(false);
          this.loading.set(false);
        }
      },
      error: () => {
        this.loadError.set('Task not found or could not be loaded.');
        this.loading.set(false);
        this.lookupsLoading.set(false);
      }
    });
  }

  private loadUsersWhileEditing(): void {
    this.usersService.getUsers().subscribe({
      next: (u) => {
        this.users.set(u);
        this.lookupsLoading.set(false);
        this.loading.set(false);
      },
      error: () => {
        this.users.set([]);
        this.lookupsLoading.set(false);
        this.loading.set(false);
      }
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saveError.set(null);
    const raw = this.form.getRawValue();
    const payload = {
      title: raw.title.trim(),
      description: raw.description.trim() ? raw.description.trim() : null,
      priority: raw.priority.trim(),
      status: raw.status.trim(),
      assignedToUserId: raw.assignedToUserId.trim()
    };

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.backlogService.update(id, payload).subscribe({
        next: () => void this.router.navigateByUrl('/backlog'),
        error: () => this.saveError.set('Could not save changes. Check your input and try again.')
      });
    } else {
      this.backlogService.create(payload).subscribe({
        next: () => void this.router.navigateByUrl('/backlog'),
        error: () => this.saveError.set('Could not create task. Check your input and try again.')
      });
    }
  }

  cancel(): void {
    void this.router.navigateByUrl('/backlog');
  }

  showAssigneeSelect(): boolean {
    return this.auth.isRootAdmin();
  }
}
