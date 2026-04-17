import { CanDeactivateFn } from '@angular/router';

export interface HasUnsavedScheduleChanges {
  hasUnsavedChanges(): boolean;
}

export const scheduleUnsavedChangesGuard: CanDeactivateFn<HasUnsavedScheduleChanges> = (component) => {
  if (!component.hasUnsavedChanges()) {
    return true;
  }

  return window.confirm("Changes haven't been saved.");
};
