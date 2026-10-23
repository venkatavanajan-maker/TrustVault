
import {
  Component,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Auth } from './services/auth';
import {
  Audit,
  AuditEntry,
  AuditVerification
} from './services/audit';
import {
  Documents,
  DocumentItem,
  DocumentShare
} from './services/documents';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  showUpload = false;

  uploadTitle = '';
  selectedFile: File | null = null;
  uploading = false;
  uploadError = '';
  uploadSuccess = '';

  isLoggedIn = false;
  isRegisterMode = false;
  loading = false;
  errorMessage = '';
  successMessage = '';

  displayName = '';
  email = '';
  password = '';

  // My documents
  documents: DocumentItem[] = [];
  documentsLoading = false;
  documentsError = '';

  // Shared documents
 currentView:
  | 'dashboard'
  | 'my-documents'
  | 'shared-documents'
  | 'audit' = 'dashboard';
  sharedDocuments: DocumentItem[] = [];
  sharedDocumentsLoading = false;
  sharedDocumentsError = '';
  // Administrator audit activity
  isAdministrator = false;
  auditEntries: AuditEntry[] = [];
  auditLoading = false;
  auditError = '';
  auditVerification: AuditVerification | null = null;
  auditVerifying = false;
  // Document sharing
  shareEmails: Record<string, string> = {};
  shareRoles: Record<string, 'Viewer' | 'Editor'> = {};
  sharingDocumentIds: Record<string, boolean> = {};
  shareMessages: Record<string, string> = {};

  // People who have access to each document
  documentShares: Record<string, DocumentShare[]> = {};
  shareListLoading: Record<string, boolean> = {};
  revokingUserIds: Record<string, boolean> = {};
constructor(
  private auth: Auth,
  private documentsService: Documents,
  private auditService: Audit,
  private cdr: ChangeDetectorRef
) {
  this.isLoggedIn = this.auth.isLoggedIn();
  this.email = this.auth.getEmail() ?? '';
  this.isAdministrator =
    this.auth.getRoles().includes('Administrator');
}
 ngOnInit(): void {
  if (this.isLoggedIn) {
    this.loadDocuments();

    if (this.isAdministrator) {
      this.loadAuditEntries();
    }
  }
}
 
  // Load recent administrator audit activity
  loadAuditEntries(): void {
    if (!this.isAdministrator) {
      return;
    }

    this.auditLoading = true;
    this.auditError = '';
    this.cdr.detectChanges();

    this.auditService.getRecentEntries().subscribe({
      next: result => {
        this.auditEntries = result;
        this.auditLoading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Audit activity request failed:', error);

        this.auditLoading = false;
        this.auditError =
          error?.status === 401
            ? 'Your session has expired. Please sign in again.'
            : error?.status === 403
              ? 'Administrator access is required to view audit activity.'
              : 'Unable to load audit activity. Please try again.';

        this.cdr.detectChanges();
      }
    });
  }

  // Verify the audit hash chain
  verifyAuditChain(): void {
    if (!this.isAdministrator || this.auditVerifying) {
      return;
    }

    this.auditVerifying = true;
    this.auditVerification = null;
    this.cdr.detectChanges();

    this.auditService.verifyChain().subscribe({
      next: result => {
        this.auditVerification = result;
        this.auditVerifying = false;
        this.cdr.detectChanges();
      },
      error: error => {
        this.auditVerifying = false;

        this.auditVerification = {
          status: error?.status === 409
            ? 'TAMPERING_DETECTED'
            : 'ERROR',
          message: error?.status === 409
            ? 'The audit hash chain is invalid.'
            : error?.status === 403
              ? 'Administrator access is required.'
              : 'Unable to verify the audit hash chain.'
        };

        this.cdr.detectChanges();
      }
    });
  }

  // Load people who have permission to access one document
  loadDocumentShares(id: string): void {
    this.shareListLoading[id] = true;
    this.cdr.detectChanges();

    this.documentsService.getDocumentShares(id).subscribe({
      next: result => {
        this.documentShares[id] = result;
        this.shareListLoading[id] = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error(
          'Unable to load document shares:',
          error
        );

        this.shareListLoading[id] = false;
        this.cdr.detectChanges();
      }
    });
  }

  // Load documents shared with the signed-in user

  // Load documents belonging to the signed-in user
  loadDocuments(): void {
    this.documentsLoading = true;
    this.documentsError = '';
    this.cdr.detectChanges();

    this.documentsService.getMyDocuments().subscribe({
      next: result => {
  this.documents = result;
  this.documentsLoading = false;
  this.cdr.detectChanges();

  for (const doc of result) {
    this.loadDocumentShares(doc.id);
  }
},
      error: error => {
        console.error(
          'TrustVault documents request failed:',
          error
        );

        this.documentsLoading = false;
        this.documentsError =
          error?.status === 401
            ? 'Your session has expired. Please sign in again.'
            : error?.status === 403
              ? 'You do not have permission to view these documents.'
              : 'Unable to load documents. Please try again.';

        this.cdr.detectChanges();
      }
    });
  }

  loadSharedDocuments(): void {
    this.sharedDocumentsLoading = true;
    this.sharedDocumentsError = '';
    this.cdr.detectChanges();

    this.documentsService.getSharedWithMe().subscribe({
      next: result => {
        this.sharedDocuments = result;
        this.sharedDocumentsLoading = false;
        this.cdr.detectChanges();

        console.log(
          'TrustVault shared documents received:',
          result
        );
      },
      error: error => {
        console.error(
          'TrustVault shared documents request failed:',
          error
        );

        this.sharedDocumentsLoading = false;
        this.sharedDocumentsError =
          error?.status === 401
            ? 'Your session has expired. Please sign in again.'
            : 'Unable to load shared documents. Please try again.';

        this.cdr.detectChanges();
      }
    });
  }

  // Switch dashboard sections
  
  // Switch between dashboard sections
  setView(
    view: 'dashboard' | 'my-documents' | 'shared-documents' | 'audit'
  ): void {
    this.currentView = view;

    if (view === 'my-documents') {
      this.loadDocuments();
    } else if (view === 'shared-documents') {
      this.loadSharedDocuments();
    } else if (view === 'audit' && this.isAdministrator) {
      this.loadAuditEntries();
    }
  }

 
  // Login or register
  submitAuth(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.email.trim() || !this.password) {
      this.errorMessage =
        'Please enter your email and password.';
      return;
    }

    if (this.isRegisterMode && !this.displayName.trim()) {
      this.errorMessage = 'Please enter your full name.';
      return;
    }

    this.loading = true;

    const request = this.isRegisterMode
      ? this.auth.register({
          displayName: this.displayName.trim(),
          email: this.email.trim(),
          password: this.password
        })
      : this.auth.login({
          email: this.email.trim(),
          password: this.password
        });

    request.subscribe({
      next: () => {
        this.loading = false;

        if (this.isRegisterMode) {
          this.successMessage =
            'Account created successfully. Please sign in.';
          this.isRegisterMode = false;
          this.password = '';
        
} else {
  this.isLoggedIn = true;
  this.isAdministrator =
    this.auth.getRoles().includes('Administrator');

  this.successMessage = 'Welcome to TrustVault.';
  this.password = '';
  this.loadDocuments();

  if (this.isAdministrator) {
    this.loadAuditEntries();
  }
}
        this.cdr.detectChanges();
      },
      error: error => {
        this.loading = false;
        this.errorMessage =
          error?.error?.message ||
          error?.error?.errors?.join(' ') ||
          'Unable to complete your request. Check the backend and try again.';

        this.cdr.detectChanges();
      }
    });
  }

  // Validate a selected file
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    this.uploadError = '';
    this.uploadSuccess = '';

    if (file && file.size > 10 * 1024 * 1024) {
      this.uploadError = 'File size must not exceed 10 MB.';
      this.selectedFile = null;
      input.value = '';
      return;
    }

    this.selectedFile = file;
  }

  // Upload a document
  uploadDocument(): void {
    this.uploadError = '';
    this.uploadSuccess = '';

    if (!this.uploadTitle.trim()) {
      this.uploadError = 'Please enter a document title.';
      return;
    }

    if (!this.selectedFile) {
      this.uploadError = 'Please choose a file to upload.';
      return;
    }

    this.uploading = true;

    this.documentsService
      .uploadDocument(
        this.selectedFile,
        this.uploadTitle.trim()
      )
      .subscribe({
        next: () => {
          this.uploading = false;
          this.uploadSuccess =
            'Document uploaded successfully!';

          this.uploadTitle = '';
          this.selectedFile = null;

          this.loadDocuments();
          this.cdr.detectChanges();
        },
        error: error => {
          this.uploading = false;

          this.uploadError =
            error?.error?.message ||
            (error?.status === 401
              ? 'Your session has expired. Please sign in again.'
              : error?.status === 403
                ? 'You do not have permission to upload documents.'
                : error?.status === 413
                  ? 'The file is too large.'
                  : 'Upload failed. Please try again.');

          this.cdr.detectChanges();
        }
      });
  }

  // Download a document
  downloadFile(id: string, fileName: string): void {
    this.documentsService.downloadDocument(id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = fileName;

        document.body.appendChild(link);
        link.click();
        link.remove();

        setTimeout(() => URL.revokeObjectURL(url), 1000);
      },
      error: error => {
        console.error('Document download failed:', error);

        alert(
          error?.status === 403
            ? 'You do not have permission to download this document.'
            : 'Download failed. Please try again.'
        );
      }
    });
  }

  // Share a document with another user
  shareDocument(id: string): void {
    const email = (this.shareEmails[id] ?? '').trim();
    const role = this.shareRoles[id] ?? 'Viewer';

    this.shareMessages[id] = '';

    if (!email) {
      this.shareMessages[id] =
        'Please enter the recipient email address.';
      return;
    }

    this.sharingDocumentIds[id] = true;
    this.cdr.detectChanges();

    this.documentsService
      .shareDocument(id, email, role)
      .subscribe({
        next: () => {
          this.shareMessages[id] =
            'Document shared successfully.';

          this.shareEmails[id] = '';
          this.shareRoles[id] = 'Viewer';
          this.sharingDocumentIds[id] = false;

          // Refresh the access list after sharing
          this.loadDocumentShares(id);
          this.cdr.detectChanges();
        },
        error: error => {
          this.sharingDocumentIds[id] = false;

          this.shareMessages[id] =
            error?.error?.message ||
            (error?.status === 403
              ? 'You are not allowed to share this document.'
              : 'Unable to share the document. Please try again.');

          this.cdr.detectChanges();
        }
      });
  }

  // Revoke a user's access to a document
  revokeDocumentAccess(
    documentId: string,
    userId: string
  ): void {
    if (this.revokingUserIds[userId]) {
      return;
    }

    this.shareMessages[documentId] = '';
    this.revokingUserIds[userId] = true;
    this.cdr.detectChanges();

    this.documentsService
      .revokeDocumentAccess(documentId, userId)
      .subscribe({
        next: () => {
          this.revokingUserIds[userId] = false;
          this.shareMessages[documentId] =
            'Document access revoked successfully.';

          // Refresh the list to remove the revoked recipient
          this.loadDocumentShares(documentId);
          this.cdr.detectChanges();
        },
        error: error => {
          this.revokingUserIds[userId] = false;

          this.shareMessages[documentId] =
            error?.error?.message ||
            (error?.status === 403
              ? 'You are not allowed to revoke this access.'
              : error?.status === 404
                ? 'The document or permission was not found.'
                : 'Unable to revoke access. Please try again.');

          this.cdr.detectChanges();
        }
      });
  }

  // Sign out and clear dashboard data
  logout(): void {
    this.auth.logout();

    this.isLoggedIn = false;
    this.showUpload = false;
    this.password = '';
    this.successMessage = '';
    this.errorMessage = '';

    this.documents = [];
    this.documentsError = '';
    this.documentsLoading = false;

    this.sharedDocuments = [];
    this.sharedDocumentsError = '';
    this.sharedDocumentsLoading = false;

    this.documentShares = {};
    this.shareListLoading = {};
    this.revokingUserIds = {};
    this.shareMessages = {};
    this.shareEmails = {};
    this.shareRoles = {};
    this.sharingDocumentIds = {};

    this.uploadTitle = '';
    this.selectedFile = null;
    this.uploading = false;
    this.uploadError = '';
    this.uploadSuccess = '';

    this.currentView = 'dashboard';
    this.cdr.detectChanges();
  }

  // Switch between login and registration
  toggleAuthMode(): void {
    this.isRegisterMode = !this.isRegisterMode;
    this.errorMessage = '';
    this.successMessage = '';
    this.password = '';
  }
}
