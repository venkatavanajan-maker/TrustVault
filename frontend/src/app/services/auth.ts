
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

const API_URL = 'http://localhost:5062/api/Auth';

export interface AuthResponse {
  message: string;
  userId: string;
  email: string;
  roles?: string[];
  role?: string;
  token?: string;
  tokenType?: string;
  expiresInMinutes?: number;
}

export interface RegisterRequest {
  displayName: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

@Injectable({
  providedIn: 'root'
})
export class Auth {
  constructor(private http: HttpClient) {}

  login(data: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_URL}/login`, data)
      .pipe(
        tap(response => {
          if (response.token) {
            sessionStorage.setItem('trustvault_token', response.token);
            sessionStorage.setItem('trustvault_email', response.email);
            sessionStorage.setItem(
              'trustvault_roles',
              JSON.stringify(response.roles ?? [])
            );
          }
        })
      );
  }

  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${API_URL}/register`,
      data
    );
  }

  getToken(): string | null {
    return sessionStorage.getItem('trustvault_token');
  }

  getEmail(): string | null {
    return sessionStorage.getItem('trustvault_email');
  }

  getRoles(): string[] {
    try {
      return JSON.parse(
        sessionStorage.getItem('trustvault_roles') ?? '[]'
      );
    } catch {
      return [];
    }
  }

  logout(): void {
    sessionStorage.removeItem('trustvault_token');
    sessionStorage.removeItem('trustvault_email');
    sessionStorage.removeItem('trustvault_roles');
  }

  isLoggedIn(): boolean {
    return this.getToken() !== null;
  }
}
