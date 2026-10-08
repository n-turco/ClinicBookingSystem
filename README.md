# Clinic Booking System

## Project Overview

Clinic Booking System is an ASP.NET Core MVC web application for scheduling clinic appointments, managing patient bookings and storing patient documents. Login and registration use the ASP.NET Core Identity Razor Pages UI. Entity Framework Core with SQL Server handles data storage.

It was built for SECU2000 (Application Security), so it puts particular weight on authorization, input handling and secure defaults.

## Key Features

**Patients (User role)**
- Register, log in and log out
- Browse and search available appointments, then book one
- View their own bookings, change a booking to another free slot, or cancel it (the slot is released again)
- Upload documents (PDF, images, text/CSV, Word, Excel, JSON; max 5 MB) and view or download their own uploads

**Administrators (Admin role)**
- Dashboard with system totals
- Create, edit and delete appointment slots
- View all bookings and all registered users
- Manage uploaded file records (view, rename, delete)

## Technical Stack

- .NET 10, ASP.NET Core MVC (controllers + views)
- ASP.NET Core Identity Razor Pages for login and registration (custom `AppUser`, `IdentityRole`)
- Entity Framework Core 10 with SQL Server (LocalDB by default)
- Bootstrap for the UI

## Project Structure

```
ClinicBookingSystem/
├── Areas/Identity/       Login, Register and Logout pages
├── Controllers/          Admin, Appointments, Bookings, Files, UploadedFiles, Home
├── Data/                 DbContext and SeedData
├── Migrations/           EF Core migrations
├── Models/               Appointment, Booking, UploadedFile, AppUser, view models
├── Services/             NoOpEmailSender (placeholder email sender)
├── Views/                MVC views
└── UploadedFiles/        Uploaded documents (created at runtime, git-ignored)
```

## Security Controls

**Authentication and sessions**
- Accounts are managed by ASP.NET Core Identity, and each email can only be used once
- Accounts are locked for 15 minutes after 5 failed login attempts
- The auth cookie is `HttpOnly` and `SameSite=Strict`, with a 30-minute sliding expiry
- HTTPS redirection is always on, with HSTS outside Development

**Authorization**
- Access is role-based (`Admin` / `User`), using `[Authorize]` on controllers and actions
- Ownership checks stop users from viewing, editing or deleting other users' bookings and files
- File-record management is admin-only

**Input and data handling**
- All state-changing POSTs carry anti-forgery (CSRF) tokens
- `[Bind]` allow-lists stop clients from overwriting server-managed fields such as owner and file path
- Booking changes are checked so a user can't double-book or take an already booked slot
- Uploads are stored outside `wwwroot` with GUID-prefixed names and served only through an authenticated, owner-checked endpoint
- Download paths are resolved and confined to the uploads folder, which blocks path traversal
- Uploads are capped by size and limited to an allow-list of types

**Logging**
- Logging uses ASP.NET Core's built-in `ILogger<T>` with structured message templates
- Security-relevant events, such as denied file access and rejected uploads, are logged with IDs only
- User-supplied file names and server paths are never written to the logs
- Unhandled exceptions are logged by the error handler

**Configuration**
- Demo accounts are only seeded in Development
- The production admin password comes from configuration, never from source code
- Uploaded files are excluded from source control

## Demo Credentials (Development only)

These accounts are created by `SeedData.InitializeAsync` **only when `ASPNETCORE_ENVIRONMENT=Development`**. In any other environment no demo users are created. There, the admin account is seeded only when `Seed:AdminPassword` (and optionally `Seed:AdminEmail`) is supplied through user-secrets or environment variables:

```
dotnet user-secrets set "Seed:AdminPassword" "<strong password>" --project ClinicBookingSystem
```

| Role  | Email              | Password    |
|-------|--------------------|-------------|
| Admin | `admin@clinic.com` | `Admin123!` |
| User  | `user@clinic.com`  | `User123!`  |

Additional sample users: `BobB@clinic.com`, `NickT@clinic.com`, `SaraS@clinic.com`, `JohnH@clinic.com` (password `User123!`).

## How to Run (Local Development)

1. Make sure SQL Server LocalDB is installed (it ships with Visual Studio). If you use another SQL Server, update the `ClinicBookingSystemContext` connection string in `appsettings.json`.
2. Restore and build from the repository root:

   ```
   dotnet restore
   dotnet build
   ```

3. Create the database:

   ```
   dotnet ef database update --project ClinicBookingSystem
   ```

4. Run the app:

   ```
   dotnet run --project ClinicBookingSystem --launch-profile https
   ```

5. Browse to `https://localhost:7015` (or `http://localhost:5250`). The demo accounts are seeded on first start.

## Roadmap

### Next: security hardening
- **Upload validation:** check the file extension and file signature (magic bytes), and set the stored content type on the server
- **Downloads:** encode file names safely in download headers
- **Response headers:** add a Content Security Policy, `X-Content-Type-Options`, `X-Frame-Options` and `Referrer-Policy`
- **Booking concurrency:** add a concurrency token plus a unique index on bookings, so a slot can only be booked once under load; reject bookings for past appointments
- **Accounts:** stronger password policy, real email delivery and required email confirmation
- **Rate limiting:** limit requests to login, registration and upload
- **Cookies:** `Secure`-only, `__Host-` prefixed auth and anti-forgery cookies
- **Data Protection:** persist the keys
- **Hosts:** restrict `AllowedHosts` in production

### Planned features and quality
- **File management:** users can delete their own uploads, which also removes the stored file
- **Appointments:** validation for overlapping slots and past dates; safe deletion of booked slots
- **Booking history:** booking status (Booked / Cancelled / Completed) instead of hard deletes
- **Role-aware UI:** admin-only links hidden from regular users, plus success and error messages
- **Tests:** xUnit test project with an authorization test across every route, plus booking and upload rule tests
- **CI:** GitHub Actions running build, tests, a vulnerable-package check and static analysis
- **Clinic data:** encryption at rest for uploaded documents, and an audit log of admin actions and file access

## Developer

Nicholas Turco
