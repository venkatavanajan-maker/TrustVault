# TrustVault — Threat Model



## 1. Project Overview



TrustVault is a document vault built with Angular, ASP.NET Core, and SQL Server. It supports authenticated access, document upload and download, document sharing, permission revocation, and audit logging.



## 2. Assets to Protect



- User credentials and authentication tokens

- Stored documents and document metadata

- Document ownership and sharing permissions

- Audit records and their integrity

- Database connection credentials and signing secrets



## 3. Threats and Mitigations




| Threat | Mitigation | Verification status |
|---|---|---|
| Unauthenticated API access | Protected API endpoints require authentication. | Pending: test requests without a token. |
| Unauthorized document download | The API checks document ownership and sharing permissions. | Partially verified: shared-document download worked for an authorized recipient. Test denial for a user without access remains pending. |
| Access after permission revocation | Download authorization is checked against current database permissions. | Verified: download attempt after revocation returned HTTP 404. |
| Unauthorized document sharing or revocation | Sharing and revocation enforce document ownership. | Pending: test both actions as a non-owner. |
| Oversized upload | The upload endpoint enforces a 10 MB limit. | Pending: test boundary sizes and confirm rejection above the limit. |
| Unsafe or misleading filenames | The server uses a generated storage key rather than the supplied filename as the storage key. | Pending: test unusual filenames and content types. |
| Audit log tampering | Audit entries use a hash chain with a verification endpoint. | Partially verified: chain verification returned VALID and 2 backend tests passed. Deliberate tampering detection still needs a safe test. |
| Credential or token exposure | Secrets should remain outside source control, and tokens must not be logged or committed. | Pending: review configuration and repository contents. |

## 4. Security Limitations and Follow-up



- Verify file content and permitted file types; a filename or declared content type alone does not prove a file is safe.

- Confirm HTTPS is used in deployment.

- Keep document storage outside publicly served directories and restrict operating-system access.

- Use least-privilege database credentials and keep secrets out of source control.

- Test authorization, revocation, upload limits, and audit-chain verification before release.

- Back up documents and audit records securely.



## 5. Responsible Testing



Use only accounts and documents created for authorized testing. Do not test against other people's files or systems. Record the result of each security test and fix confirmed vulnerabilities before deployment.



