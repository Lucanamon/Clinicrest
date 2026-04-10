import { isPlatformBrowser } from '@angular/common';
import { Component, Inject, OnInit, PLATFORM_ID, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../services/auth';
import { UsersService } from '../users/users.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly usersService = inject(UsersService);
  private readonly auth = inject(AuthService);

  readonly defaultAvatar = 'https://ui-avatars.com/api/?name=User';
  readonly saving = signal(false);
  readonly loading = signal(true);
  readonly saveError = signal<string | null>(null);
  readonly success = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    displayName: ['', [Validators.maxLength(120)]],
    profileImageUrl: ['', [Validators.maxLength(2048)]]
  });

  constructor(@Inject(PLATFORM_ID) private readonly platformId: object) {}

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) {
      this.loading.set(false);
      return;
    }

    this.usersService.getMyProfile().subscribe({
      next: (profile) => {
        this.form.patchValue({
          displayName: profile.displayName ?? '',
          profileImageUrl: profile.profileImageUrl ?? ''
        });
        this.auth.setCurrentUserProfile(profile);
        this.loading.set(false);
      },
      error: () => {
        this.saveError.set('Could not load your profile.');
        this.loading.set(false);
      }
    });
  }

  save(): void {
    const raw = this.form.getRawValue();
    const profileImageUrl = raw.profileImageUrl.trim();

    if (profileImageUrl && !profileImageUrl.startsWith('http')) {
      this.saveError.set('Invalid image URL. It should start with http or https.');
      return;
    }

    this.saving.set(true);
    this.saveError.set(null);
    this.success.set(null);

    this.usersService
      .updateMyProfile({
        displayName: raw.displayName.trim() || null,
        profileImageUrl: profileImageUrl || null
      })
      .subscribe({
        next: (profile) => {
          this.auth.setCurrentUserProfile(profile);
          this.saving.set(false);
          this.success.set('Profile updated.');
        },
        error: (err: { error?: { message?: string } }) => {
          this.saving.set(false);
          this.saveError.set(err?.error?.message ?? 'Could not update profile.');
        }
      });
  }

  onImageError(event: Event): void {
    const target = event.target as HTMLImageElement | null;
    if (target) {
      target.src = this.defaultAvatar;
    }
  }
}
