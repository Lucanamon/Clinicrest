import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from '../services/auth';
import { GlobalSearchComponent } from './global-search/global-search.component';
import { RightSidebarComponent } from '../right-sidebar/right-sidebar.component';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, GlobalSearchComponent, RightSidebarComponent],
  templateUrl: './dashboard-layout.component.html',
  styleUrl: './dashboard-layout.component.scss'
})
export class DashboardLayoutComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly defaultAvatar = 'https://ui-avatars.com/api/?name=User';
  private readonly profile = toSignal(this.auth.currentUserProfile$, {
    initialValue: this.auth.getCurrentUserProfile()
  });

  readonly displayName = computed(() => {
    const profile = this.profile();
    return profile?.displayName?.trim() || profile?.username || this.auth.getUsername();
  });

  readonly avatarUrl = computed(() => {
    const profile = this.profile();
    return profile?.profileImageUrl?.trim() || this.defaultAvatar;
  });

  get canAccessBooking(): boolean {
    const role = this.auth.getRole();
    return role === 'Doctor' || role === 'Administrator' || role === 'RootAdmin';
  }

  get isGuest(): boolean {
    return this.auth.isGuest();
  }

  goToProfile(): void {
    void this.router.navigateByUrl('/profile');
  }

  logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }

  onImageError(event: Event): void {
    const target = event.target as HTMLImageElement | null;
    if (target) {
      target.src = this.defaultAvatar;
    }
  }
}
