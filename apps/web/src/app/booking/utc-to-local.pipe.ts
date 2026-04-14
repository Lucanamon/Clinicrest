import { formatDate } from '@angular/common';
import { inject, LOCALE_ID, Pipe, PipeTransform } from '@angular/core';

/**
 * Booking/slot APIs return instants in UTC (ISO-8601 with Z or +00:00).
 * Use this pipe in templates for display only — do not send local times back to the API.
 */
@Pipe({
  name: 'utcToLocal',
  standalone: true,
})
export class UtcToLocalPipe implements PipeTransform {
  private readonly locale = inject(LOCALE_ID);

  transform(value: string | Date | null | undefined, format = 'medium'): string {
    if (value == null || value === '') {
      return '';
    }
    const d = typeof value === 'string' ? new Date(value) : value;
    if (Number.isNaN(d.getTime())) {
      return '';
    }
    return formatDate(d, format, this.locale);
  }
}
