
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

const API_URL = (typeof window !== 'undefined' &&
  window.location.hostname === 'localhost')
  ? 'http://localhost:5062/api/Documents'
  : 'https://trustvault-g7zg.onrender.com/api/Documents';

export interface DocumentItem {
  id: string;
  title: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  createdAtUtc: string;
}

export interface DocumentShare {
  userId: string;
  email: string | null;
  role: string;
  grantedAtUtc: string;
}

@Injectable({
  providedIn: 'root'
})
export class Documents {
  constructor(private http: HttpClient) {}

  getMyDocuments(): Observable<DocumentItem[]> {
    return this.http.get<DocumentItem[]>(API_URL);
  }

  uploadDocument(file: File, title: string): Observable<unknown> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('title', title);

    return this.http.post(`${API_URL}/upload`, formData);
  }

  getSharedWithMe(): Observable<DocumentItem[]> {
    return this.http.get<DocumentItem[]>(
      `${API_URL}/shared-with-me`
    );
  }
getDocumentShares(id: string): Observable<DocumentShare[]> {
  return this.http.get<DocumentShare[]>(
    `${API_URL}/${id}/shares`
  );
}
  downloadDocument(id: string): Observable<Blob> {
    return this.http.get(
      `${API_URL}/${id}/download`,
      { responseType: 'blob' }
    );
  }
  shareDocument(
    id: string,
    email: string,
    role: 'Viewer' | 'Editor'
  ): Observable<unknown> {
    return this.http.post(`${API_URL}/${id}/share`, {
      email,
      role
    });
  }

  revokeDocumentAccess(
    id: string,
    userId: string
  ): Observable<unknown> {
    return this.http.delete(
      `${API_URL}/${id}/share/${encodeURIComponent(userId)}`
    );
  }
}

