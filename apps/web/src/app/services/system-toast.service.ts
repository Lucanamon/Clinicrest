import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class SystemToastService {
  private clearTimer: ReturnType<typeof setTimeout> | undefined;
  private readonly _message = signal<string | null>(null);
  readonly message = this._message.asReadonly();

  show(text: string, durationMs = 5000): void {
    if (this.clearTimer) {
      clearTimeout(this.clearTimer);
    }
    this._message.set(text);
    this.clearTimer = setTimeout(() => {
      this._message.set(null);
      this.clearTimer = undefined;
    }, durationMs);
  }
}
