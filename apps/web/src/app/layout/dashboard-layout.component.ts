import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
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

  get username(): string {
    return this.auth.getUsername();
  }

  get displayName(): string {
    const profile = this.auth.getCurrentUserProfile();
    return profile?.displayName?.trim() || profile?.username || this.username;
  }

  get profileImageUrl(): string | null {
    const profile = this.auth.getCurrentUserProfile();
    return profile?.profileImageUrl?.trim() || null;
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
