
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

const API_URL = 'http://localhost:5062/api/Audit';

export interface AuditEntry {
  id: number;
  timestampUtc: string;
  userId: string | null;
  action: string;
  resourceType: string;
  resourceId: string;
  succeeded: boolean;
  details: string;
}

export interface AuditVerification {
  status: string;
  message: string;
  invalidEntryId?: number;
}

@Injectable({
  providedIn: 'root'
})
export class Audit {
  constructor(private http: HttpClient) {}

  getRecentEntries(): Observable<AuditEntry[]> {
    return this.http.get<AuditEntry[]>(API_URL);
  }

  verifyChain(): Observable<AuditVerification> {
    return this.http.get<AuditVerification>(
      `${API_URL}/verify`
    );
  }
}
