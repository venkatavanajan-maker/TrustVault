\# TrustVault — Threat Model



\## 1. Project Overview



TrustVault is a document vault built with Angular, ASP.NET Core, and SQL Server. It supports authenticated access, document upload and download, document sharing, permission revocation, and audit logging.



\## 2. Assets to Protect



\- User credentials and authentication tokens

\- Stored documents and document metadata

\- Document ownership and sharing permissions

\- Audit records and their integrity

\- Database connection credentials and signing secrets



\## 3. Threats and Mitigations



| Threat | Mitigation | Verification Needed |

|---|---|---|

| Unauthenticated API access | Authentication is required for document and audit endpoints. | Test requests without a token. |

| Unauthorized document download | The download endpoint checks document ownership and Viewer/Editor permissions. | Test using a user without access. |

| Access after permission revocation | Download permission is checked against the database on each request. | Revoke access and immediately retry the download. |

| Unauthorized document sharing or revocation | Sharing and revocation check document ownership. | Test as a non-owner. |

| Oversized upload | The upload endpoint enforces a 10 MB file limit. | Test boundary sizes. |

| Unsafe or misleading filenames | The server extracts the original filename and uses a generated storage key for storage. | Test unusual filenames and content types. |

| Audit log tampering | Audit entries use a hash chain with a verification endpoint. | Run verification and test integrity controls in a safe test environment. |

| Credential or token exposure | Secrets should be stored outside source control, and tokens must not be logged or committed. | Review configuration and repository contents. |



\## 4. Security Limitations and Follow-up



\- Verify file content and permitted file types; a filename or declared content type alone does not prove a file is safe.

\- Confirm HTTPS is used in deployment.

\- Keep document storage outside publicly served directories and restrict operating-system access.

\- Use least-privilege database credentials and keep secrets out of source control.

\- Test authorization, revocation, upload limits, and audit-chain verification before release.

\- Back up documents and audit records securely.



\## 5. Responsible Testing



Use only accounts and documents created for authorized testing. Do not test against other people's files or systems. Record the result of each security test and fix confirmed vulnerabilities before deployment.



