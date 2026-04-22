import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SystemToastComponent } from './components/system-toast/system-toast.component';
import { AuthService } from './services/auth';
import { RealTimeService } from './services/real-time.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SystemToastComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly auth = inject(AuthService);

  constructor() {
    inject(RealTimeService);
  }

  ngOnInit(): void {
    const restored = this.auth.restoreSession();
    if (restored) {
      this.auth.loadCurrentUserProfile().subscribe();
    }
  }
}
