import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PatientDto {
  id: string;
  name: string;
  age: number;
  phone: string;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  constructor(private http: HttpClient) {}

  getTest() {
    return this.http.get('/api/test');
  }

  getPatients(): Observable<PatientDto[]> {
    return this.http.get<PatientDto[]>('/api/patients');
  }
}
