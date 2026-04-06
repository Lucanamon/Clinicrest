import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DoctorListItemDto {
  id: string;
  username: string;
}

export interface UserListItemDto {
  id: string;
  username: string;
  role: string;
}

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private readonly http = inject(HttpClient);

  getDoctors(): Observable<DoctorListItemDto[]> {
    return this.http.get<DoctorListItemDto[]>('/api/users/doctors');
  }

  getUsers(): Observable<UserListItemDto[]> {
    return this.http.get<UserListItemDto[]>('/api/users');
  }
}
