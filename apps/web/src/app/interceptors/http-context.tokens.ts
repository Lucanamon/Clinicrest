import { HttpContextToken } from '@angular/common/http';

/** When true, the global error interceptor should not open an alert (caller handles UI). */
export const skipGlobalErrorAlert = new HttpContextToken<boolean>(() => false);
