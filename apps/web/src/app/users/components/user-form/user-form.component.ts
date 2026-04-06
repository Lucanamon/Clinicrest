import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { UsersService } from '../../users.service';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './user-form.component.html',
  styleUrl: './user-form.component.scss'
})
export class UserFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly usersService = inject(UsersService);
  private readonly router = inject(Router);

  readonly saving = signal(false);
  readonly saveError = signal<string | null>(null);

  readonly roleOptions = ['Doctor', 'Nurse', 'Administrator'] as const;

  readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.maxLength(100)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(256)]],
    role: ['Doctor', [Validators.required]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saveError.set(null);
    this.saving.set(true);
    const v = this.form.getRawValue();
    this.usersService
      .createUser({
        username: v.username.trim(),
        password: v.password,
        role: v.role
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          void this.router.navigateByUrl('/users');
        },
        error: (err: { error?: { message?: string } }) => {
          this.saving.set(false);
          const msg = err?.error?.message ?? 'Could not create user.';
          this.saveError.set(msg);
        }
      });
  }

  cancel(): void {
    void this.router.navigateByUrl('/users');
  }
}
