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
    const contact = body.phoneNumber.trim();
    const payload =
      body.channel === 'Email'
        ? {
            emailAddress: contact,
            message: body.message.trim(),
            channel: body.channel,
          }
        : {
            phoneNumber: contact,
            message: body.message.trim(),
            channel: body.channel,
          };
    console.debug('[NotificationTestService] request payload', payload);

    return this.http.post<TestSendNotificationResponse>(
      `${environment.apiUrl}/notifications/test-send`,
      payload,
      { context: this.context }
    );
  }
}
