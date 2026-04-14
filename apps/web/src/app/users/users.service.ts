import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface DoctorListItemDto {
  id: string;
  username: string;
}

export interface UserDto {
  id: string;
  username: string;
  displayName?: string | null;
  role: string;
  profileImageUrl?: string | null;
  lastActiveAt: string;
  createdAt: string;
}

export interface CreateUserRequest {
  username: string;
  password: string;
  role: string;
  profileImageUrl?: string | null;
}

export interface UpdateProfileRequest {
  displayName?: string | null;
  profileImageUrl?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/users`;

  getDoctors(): Observable<DoctorListItemDto[]> {
    return this.http.get<DoctorListItemDto[]>(`${this.baseUrl}/doctors`);
  }

  getUsers(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(this.baseUrl);
  }

  getMyProfile(): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.baseUrl}/me`);
  }

  updateMyProfile(request: UpdateProfileRequest): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.baseUrl}/me`, request);
  }

  createUser(request: CreateUserRequest): Observable<UserDto> {
    return this.http.post<UserDto>(this.baseUrl, request);
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
