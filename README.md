# Clinic Booking System

## Project Overview

Clinic Booking System is a Razor Pages web application for scheduling and managing clinic appointments, patient bookings, and file uploads. It uses ASP.NET Core Identity for authentication and role-based authorization, and Entity Framework Core with SQL Server for data persistence.

## Key Features

- User registration and email confirmation flow
- Role-based access control (Admin, User)
- Browse and book available appointments
- User-specific bookings management
- File upload, view, download, and delete for user files
- Seeded sample data for quick local demo

## Technical Stack

- .NET 10 (ASP.NET Core Razor Pages)
- ASP.NET Core Identity (custom `AppUser` with `IdentityRole`)
- Entity Framework Core (SQL Server provider)
- Razor Pages for UI
- Dependency injection and configuration via `Program.cs`

## Security Enhancements

- Authentication using ASP.NET Core Identity with EF Core stores
- Role-based authorization and seeded `Admin`/`User` roles
- Unique email enforcement for user accounts
- Cookie security settings: `HttpOnly`, `SameSite=Strict`, sliding expiration, and 30-minute timeout
- HTTPS enforcement and HSTS in production
- Input validation on registration (data annotations)
- Email confirmation flow implemented (seeded accounts may be pre-confirmed)
- Referential integrity enforced by Identity schema (preventing orphaned role links)

## Demo Credentials

> These seeded demo accounts are created by `SeedData.InitializeAsync` for local development. Change or remove them before publishing to production.

- Administrator
  - Email: `admin@clinic.com`
  - Password: `Admin123!`

- Regular User
  - Email: `user@clinic.com`
  - Password: `User123!`

Additional sample users: `BobB@clinic.com`, `NickT@clinic.com`, `SaraS@clinic.com`, `JohnH@clinic.com` (password `User123!`).

## How to Run (Local Development)

1. Ensure SQL Server is available and update the connection string named `ClinicBookingSystemContext` in `appsettings.json` if necessary.
2. From the project folder, restore and build:

   dotnet restore
   dotnet build

3. Apply EF Core migrations (if any) and update the database:

   dotnet ef database update

4. Run the application:

   dotnet run

5. Open a browser and navigate to `https://localhost:5001` (or the port printed by the app). The seeded demo accounts will be available after the initial seed runs.

## Developer Credentials / Notes

- Project owner / maintainer account: use the seeded `admin@clinic.com` for administrative tasks during local development.
- To create a developer account or reset credentials, use the `UserManager` APIs in code, or remove the seeded users/roles in `SeedData` and create new ones.
- Seed data is executed during application startup (see `Program.cs`) inside a scoped service provider — modify `SeedData.InitializeAsync` if you want different initial users/roles or sample data.
- Keep the `NoOpEmailSender` only for development; replace with a real `IEmailSender` implementation for sending confirmation emails in staging/production.

---
