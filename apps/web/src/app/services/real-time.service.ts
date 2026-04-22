import { isPlatformBrowser } from '@angular/common';
import { inject, Injectable, NgZone, OnDestroy, PLATFORM_ID } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subscription } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth';
import { SystemToastService } from './system-toast.service';

@Injectable({
  providedIn: 'root'
})
export class RealTimeService implements OnDestroy {
  private connection: HubConnection | null = null;
  private authSub?: Subscription;
  private startChain: Promise<void> = Promise.resolve();

  constructor() {
    const platformId = inject(PLATFORM_ID);
    if (!isPlatformBrowser(platformId)) {
      return;
    }

    const auth = inject(AuthService);
    const toast = inject(SystemToastService);
    const zone = inject(NgZone);

    this.authSub = auth.authState$.subscribe((isAuthed) => {
      this.startChain = this.startChain.then(async () => {
        if (isAuthed) {
          await this.startConnection(auth, toast, zone);
        } else {
          await this.stopConnection();
        }
      });
    });
  }

  ngOnDestroy(): void {
    this.authSub?.unsubscribe();
    void this.stopConnection();
  }

  private buildHubUrl(): string {
    const base = environment.apiBaseUrl?.trim();
    if (base) {
      return `${base.replace(/\/$/, '')}/hubs/notifications`;
    }
    if (typeof globalThis !== 'undefined' && 'location' in globalThis) {
      const loc = (globalThis as { location: { origin: string } }).location;
      return `${loc.origin}/hubs/notifications`;
    }
    return '/hubs/notifications';
  }

  private async startConnection(
    auth: AuthService,
    toast: SystemToastService,
    zone: NgZone
  ): Promise<void> {
    await this.stopConnection();
    const token = auth.getToken();
    if (!token) {
      return;
    }

    const url = this.buildHubUrl();
    const connection = new HubConnectionBuilder()
      .withUrl(url, { accessTokenFactory: () => auth.getToken() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(environment.production ? LogLevel.Warning : LogLevel.Information)
      .build();

    connection.on('ReceiveSystemAlert', (message: string) => {
      zone.run(() => {
        toast.show(String(message), 5000);
      });
    });

    this.connection = connection;
    try {
      await connection.start();
    } catch (err) {
      console.error('Real-time (SignalR) connection failed', err);
    }
  }

  private async stopConnection(): Promise<void> {
    if (!this.connection) {
      return;
    }
    const c = this.connection;
    this.connection = null;
    try {
      await c.stop();
    } catch {
      // ignore
    }
  }
}
