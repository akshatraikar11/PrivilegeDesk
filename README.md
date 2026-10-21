# PrivilegeDesk

A small Privileged Access Management (PAM) demo for .NET 8 on Windows.

Employees request time-boxed access to sensitive resources. Approvers decide in a WinForms app. A Windows Service expires access when the clock runs out. Everything is audited.

The product name is **PrivilegeDesk** (clearer than a shortened “PrivAccess”) so it is easy to talk about in an interview.

## Requirements

### Functional requirements

1. Users sign in with a username and password and receive a JWT.
2. Employees can create an access request for a named resource and a duration.
3. Employees can list their own requests.
4. Approvers and Admins can list pending requests (from a SQL stored procedure).
5. Approvers and Admins can approve or reject a pending request. Approval sets `ExpiresAt`.
6. Admins can read the audit log.
7. Every login, request, approval, rejection, and expiry writes an audit row.
8. A background Windows Service marks approved requests as `Expired` after `ExpiresAt`.

### User stories

1. **As an employee**, I want to request access to the Prod DB Server for 60 minutes so I can finish a production hotfix.
2. **As an employee**, I want to see the status of my own requests so I know whether I am still waiting.
3. **As an approver**, I want a desktop list of pending requests so I can approve or reject them without using Swagger.
4. **As an approver**, I want an approved request to expire automatically so leftover privileged access does not stay open.
5. **As an admin**, I want to read an audit trail of logins and decisions so I can answer “who approved what?”.
6. **As an operator**, I want the expiry job to run as a Windows Service so it keeps working after I log off.

## Architecture

```
Employee (Swagger / HTTP)  -->  PrivilegeDesk.Api  -->  SQL Server LocalDB
Approver (WinForms)        -->  PrivilegeDesk.Api          ^
PrivilegeDesk.ExpiryService (Windows Service)  -------------+
```

| Project | What it is |
| --- | --- |
| `PrivilegeDesk.Api` | ASP.NET Core Web API: JWT auth, EF Core, Swagger |
| `PrivilegeDesk.Admin` | WinForms approver client using `HttpClient` |
| `PrivilegeDesk.ExpiryService` | .NET Worker with `UseWindowsService()`, Channels, and two consumers |

Roles: `Employee`, `Approver`, `Admin`.

Seeded users (password for all: `Password123!`):

| Username | Role |
| --- | --- |
| `employee` | Employee |
| `approver` | Approver |
| `admin` | Admin |

Seeded resources: Prod DB Server, Linux Jump Host, AWS Root Console, Payroll File Share.

## Setup

### 1. Prerequisites

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (or `dotnet` CLI)
- SQL Server LocalDB (Visual Studio workload **ASP.NET and web development**, or SQL Server Express with the LocalDB feature)

Check LocalDB:

```powershell
sqllocaldb info MSSQLLocalDB
```

If it is stopped:

```powershell
sqllocaldb start MSSQLLocalDB
```

### 2. Connection string

Both the API and the worker use LocalDB in `appsettings.json`:

```
Server=(localdb)\mssqllocaldb;Database=PrivilegeDesk;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

### 3. Restore, migrate, run the API

From the solution folder:

```powershell
dotnet restore
dotnet tool restore
dotnet tool run dotnet-ef database update --project PrivilegeDesk.Api --startup-project PrivilegeDesk.Api
dotnet run --project PrivilegeDesk.Api
```

A first migration (`InitialCreate`) is already in the repo. To add another later:

```powershell
dotnet tool run dotnet-ef migrations add NameHere --project PrivilegeDesk.Api --startup-project PrivilegeDesk.Api
```

The API also calls `Database.Migrate()` on startup, then seeds users, resources, and stored procedure `dbo.GetPendingRequests`.

Swagger: [http://localhost:5080/swagger](http://localhost:5080/swagger)

### 4. Run the WinForms client

Keep the API running, then:

```powershell
dotnet run --project PrivilegeDesk.Admin
```

Or set **Multiple startup projects** in Visual Studio (API + Admin). Sign in as `approver` / `Password123!`.

The client talks to `http://localhost:5080` and keeps the JWT in memory only.

### 5. Run the expiry worker (console, for debugging)

```powershell
dotnet run --project PrivilegeDesk.ExpiryService
```

It scans every 30 seconds. To see it work, approve a request with a short duration (the API minimum is 15 minutes; you can temporarily lower that range in `CreateAccessRequestDto` while testing).

### 6. Install as a Windows Service (`sc.exe`)

Build a Release copy, then run **Command Prompt as Administrator**:

```powershell
dotnet publish PrivilegeDesk.ExpiryService -c Release -o C:\Services\PrivilegeDeskExpiry

sc.exe create PrivilegeDeskExpiry binPath= "C:\Services\PrivilegeDeskExpiry\PrivilegeDesk.ExpiryService.exe" start= auto
sc.exe description PrivilegeDeskExpiry "Expires approved PrivilegeDesk access requests."
sc.exe start PrivilegeDeskExpiry
```

Note the space after `binPath=` and `start=` — `sc.exe` requires it.

Stop and remove:

```powershell
sc.exe stop PrivilegeDeskExpiry
sc.exe delete PrivilegeDeskExpiry
```

The service account must be allowed to connect to LocalDB. For a student demo it is simpler to run the worker with `dotnet run` under your own account.

## Sample API calls

Login (copy `token` from the JSON):

```powershell
curl -s http://localhost:5080/api/auth/login -H "Content-Type: application/json" -d "{\"username\":\"employee\",\"password\":\"Password123!\"}"
```

Create a request (employee token):

```powershell
curl -s http://localhost:5080/api/requests -H "Authorization: Bearer EMPLOYEE_TOKEN" -H "Content-Type: application/json" -d "{\"resourceId\":1,\"reason\":\"Need prod DB for hotfix\",\"durationMinutes\":60}"
```

My requests:

```powershell
curl -s http://localhost:5080/api/requests/mine -H "Authorization: Bearer EMPLOYEE_TOKEN"
```

Pending (approver token):

```powershell
curl -s http://localhost:5080/api/requests/pending -H "Authorization: Bearer APPROVER_TOKEN"
```

Approve / reject:

```powershell
curl -s -X PUT http://localhost:5080/api/requests/1/approve -H "Authorization: Bearer APPROVER_TOKEN"
curl -s -X PUT http://localhost:5080/api/requests/1/reject -H "Authorization: Bearer APPROVER_TOKEN"
```

Audit (admin token):

```powershell
curl -s http://localhost:5080/api/audit -H "Authorization: Bearer ADMIN_TOKEN"
```

In Swagger: login, click **Authorize**, paste the token (no `Bearer ` prefix needed with this Swagger setup).

## Interview notes

### JWT flow

1. Client posts username + password.
2. API looks up the user and checks the password with BCrypt (`Verify` against `PasswordHash`).
3. API signs a JWT that contains user id, name, and role.
4. Client sends `Authorization: Bearer <token>` on later calls.
5. `[Authorize(Roles = "...")]` reads the role claim. The API does not store sessions.

### EF Core migrations

Code-first means C# classes are the source of truth. `dotnet ef migrations add` writes a C# snapshot of the model plus an `Up()` method that creates tables. `dotnet ef database update` (or `Database.Migrate()` at startup) applies those scripts to SQL Server.

### async/await vs threads

`await` does **not** mean “start a new thread”. It means “this method is waiting on I/O; free the thread until the database/HTTP call finishes.” Extra threads appear only when we ask for them (`Task.Run`) so two consumers can run at the same time.

### Channels

`System.Threading.Channels.Channel<T>` is a producer/consumer queue that is safe for several tasks. The worker’s producer writes expired request ids; two consumers read ids and update SQL. That avoids locking a `List<T>` ourselves.

### What a Windows Service is

A program the Service Control Manager can start at boot, run with no user logged in, and stop cleanly. `AddWindowsService()` hooks .NET’s generic host into that SCM lifetime (`Ctrl+C` locally, `sc stop` in production).

## Solution layout

```
PrivilegeDesk.sln
PrivilegeDesk.Api/
PrivilegeDesk.Admin/
PrivilegeDesk.ExpiryService/
README.md
```
