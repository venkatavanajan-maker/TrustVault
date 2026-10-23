

# TrustVault — Secure Document Vault



TrustVault is a secure document management web application built with Angular, ASP.NET Core, and SQL Server. It is designed to help users upload, access, share, and manage documents with server-side authorization and audit logging.



## Features



- \*\*Authentication:\*\* User registration and login using ASP.NET Core Identity and JWT authentication.

- \*\*Document management:\*\* Upload, list, and download documents through protected API endpoints.

- \*\*Document sharing:\*\* Grant and revoke document access through the application.

- \*\*Access control:\*\* Enforce document permissions on the server rather than relying only on frontend visibility.

- \*\*Audit logging:\*\* Record document-related security events and verify the integrity of the audit hash chain.

- \*\*Web dashboard:\*\* Angular interface for authentication, document management, and shared documents.



## Technology Stack



| Layer | Technology |

|---|---|

| Frontend | Angular 22 |

| Backend | ASP.NET Core Web API (.NET 10) |

| Database | Microsoft SQL Server Express |

| Authentication | ASP.NET Core Identity and JWT |

| Data access | Entity Framework Core |

| API documentation | Swagger / OpenAPI |



## Project Structure



```text

TrustVault/

├── backend/

│   └── TrustVault.Api/

├── frontend/

├── THREATS.md

├── PROMPTS.md

└── README.md

```



## Prerequisites



Install and configure the following before running the project:



- .NET 10 SDK

- Node.js and npm

- Angular CLI-compatible dependencies installed through npm

- Microsoft SQL Server Express

- A configured SQL Server database and valid application secrets



## Running the Application



### 1. Start the backend



Open a PowerShell terminal and run:



```powershell

cd C:\\Users\\VANAJA\\TrustVault\\backend\\TrustVault.Api

dotnet run

```



The configured local API address is:



`http://localhost:5062`



Swagger documentation is available at:



`http://localhost:5062/swagger/index.html`



Keep the backend terminal running.



### 2. Start the frontend



Open a second PowerShell terminal and run:



```powershell

cd C:\\Users\\VANAJA\\TrustVault\\frontend

npm.cmd install

npm.cmd start

```



Open the local address reported by Angular in the terminal. The usual development address is:



`http://localhost:4200`



If that port is already occupied, use the alternative port reported by Angular.



### 3. Database and secrets



Configure the SQL Server connection string and JWT signing secret for the local environment. Use ASP.NET Core User Secrets or an appropriate environment-specific configuration mechanism for sensitive values.



Do not commit passwords, JWT signing keys, access tokens, or other secrets to source control.



Ensure the database exists and the required Entity Framework Core migrations have been applied before using the application.



## Security Design



TrustVault is designed around the following security principles:



- Authenticate API requests before granting access to protected resources.

- Check document ownership and permissions on the server for protected operations.

- Keep document storage private and restrict access through authorized API operations.

- Revoke document permissions so subsequent requests are checked against current access rights.

- Record relevant security events in an audit log.

- Verify audit-chain integrity through the audit verification endpoint.

- Apply upload size limits and validate incoming requests.



These are intended security controls; their effectiveness depends on the implementation and configuration. See `THREATS.md` for the threat model, mitigations, limitations, and verification checklist.



## Testing and Verification



The following checks have been completed in the development environment:



- Backend build succeeded using `dotnet build`.

- Angular production build succeeded using `npm.cmd run build`.

- Frontend unit tests passed: \*\*4 tests across 3 test files\*\*.

- Audit hash-chain verification returned `VALID`.

- A download attempt by a user without access after permission revocation was denied with HTTP 404, and a failed download event was recorded in the audit log.



These results describe the specific checks performed; they do not establish that every security scenario has been tested.



### Run the tests



Backend build:



```powershell

cd C:\\Users\\VANAJA\\TrustVault\\backend\\TrustVault.Api

dotnet build

```



Frontend production build:



```powershell

cd C:\\Users\\VANAJA\\TrustVault\\frontend

npm.cmd run build

```



Frontend unit tests:



```powershell

cd C:\\Users\\VANAJA\\TrustVault\\frontend

npm.cmd test -- --watch=false

```



## Security Documentation



- \*\*`THREATS.md`\*\* — Threat model, mitigations, limitations, and security verification checklist.

- \*\*`PROMPTS.md`\*\* — Documentation of AI assistance and human review during development.



## Project Status



TrustVault is a development-stage project. Further security testing, deployment configuration, and verification of remaining threat-model checks are required before production use.



## Responsible Use



Use only test accounts and documents you are authorized to access. Do not upload sensitive real-world documents to an unreviewed development deployment.



