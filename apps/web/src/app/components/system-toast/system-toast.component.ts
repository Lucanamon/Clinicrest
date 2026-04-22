import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SystemToastService } from '../../services/system-toast.service';

@Component({
  selector: 'app-system-toast',
  template: `
    @if (toasts.message()) {
      <div class="system-toast" role="status" aria-live="polite">
        <span class="system-toast__text">{{ toasts.message() }}</span>
      </div>
    }
  `,
  styles: [
    `
      :host {
        display: block;
        pointer-events: none;
        position: fixed;
        inset: 0;
        z-index: 10000;
      }
      .system-toast {
        pointer-events: auto;
        position: absolute;
        bottom: 1.25rem;
        right: 1.25rem;
        max-width: min(24rem, calc(100vw - 2.5rem));
        padding: 0.75rem 1rem;
        border-radius: 0.5rem;
        box-shadow: 0 0.5rem 1.5rem rgba(15, 23, 42, 0.18);
        background: #0f172a;
        color: #f8fafc;
        font-size: 0.9375rem;
        line-height: 1.4;
        animation: system-toast-in 0.2s ease-out;
      }
      .system-toast__text {
        white-space: pre-wrap;
        word-break: break-word;
      }
      @keyframes system-toast-in {
        from {
          opacity: 0;
          transform: translateY(0.5rem);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SystemToastComponent {
  readonly toasts = inject(SystemToastService);
}
