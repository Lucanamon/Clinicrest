import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppointmentDto } from '../../appointment/appointment.service';
import { BacklogDto } from '../../backlog/backlog.service';
import { PatientDto } from '../../patient/patient.service';

export interface GlobalSearchResult {
  patients: PatientDto[];
  appointments: AppointmentDto[];
  backlogs: BacklogDto[];
}

@Injectable({
  providedIn: 'root'
})
export class GlobalSearchService {
  private readonly http = inject(HttpClient);

  search(query: string): Observable<GlobalSearchResult> {
    const params = new HttpParams().set('query', query);
    return this.http.get<GlobalSearchResult>('/api/search', { params });
  }
}
