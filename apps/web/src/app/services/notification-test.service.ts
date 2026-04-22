import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { skipGlobalErrorAlert } from '../interceptors/http-context.tokens';

export type TestNotificationChannel = 'Sms' | 'Email';

export interface TestSendNotificationRequest {
  phoneNumber: string;
  message: string;
  channel: TestNotificationChannel;
}

export interface TestSendNotificationResponse {
  message: string;
  senderAccepted: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationTestService {
  private readonly http = inject(HttpClient);
  private readonly context = new HttpContext().set(skipGlobalErrorAlert, true);

  testSend(body: TestSendNotificationRequest): Observable<TestSendNotificationResponse> {
    return this.http.post<TestSendNotificationResponse>(
      `${environment.apiUrl}/notifications/test-send`,
      {
        phoneNumber: body.phoneNumber.trim(),
        message: body.message.trim(),
        channel: body.channel
      },
      { context: this.context }
    );
  }
}
