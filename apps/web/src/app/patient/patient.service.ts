import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PatientDto {
  id: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  phoneNumber: string;
  createdAt: string;
}

export interface CreatePatientRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  phoneNumber: string;
}

export type UpdatePatientRequest = CreatePatientRequest;

@Injectable({
  providedIn: 'root'
})
export class PatientService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/patients';

  getAll(): Observable<PatientDto[]> {
    return this.http.get<PatientDto[]>(this.baseUrl);
  }

  getById(id: string): Observable<PatientDto> {
    return this.http.get<PatientDto>(`${this.baseUrl}/${id}`);
  }

  create(data: CreatePatientRequest): Observable<PatientDto> {
    return this.http.post<PatientDto>(this.baseUrl, data);
  }

  update(id: string, data: UpdatePatientRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
