/*
 FILE          : SeedData.cs
 PROJECT       : SECU2000 - Project
 PROGRAMMER    : Nick Turco | 9056530
 FIRST VERSION : 2026-04-12
 DESCRIPTION   : This file contains the SeedData class, which is responsible for seeding initial data into the Clinic Booking System database.
                 The class includes an asynchronous method InitializeAsync that creates default roles (Admin and User), a default admin user,
                 and several sample users. It also seeds sample appointments into the database if no appointments exist. The method uses
                 ASP.NET Core Identity to manage user and role creation, and it interacts with the database context to add appointments.
                 This seeding process ensures that the application has necessary data for testing and development purposes.

                 SECURITY: Demo users and sample appointments are only seeded in the Development environment. The admin password
                 is read from configuration ("Seed:AdminPassword", e.g. user-secrets or an environment variable). Outside
                 Development the admin account is only created when that setting is provided, so no well-known password is
                 ever deployed.
*/
using Microsoft.AspNetCore.Identity;
using ClinicBookingSystem.Models;

namespace ClinicBookingSystem.Data
{
    public static class SeedData
    {
        // Development-only fallback passwords. These are documented in the README for local demos and must never
        // be used in a deployed environment (enforced by the IsDevelopment checks below).
        private const string DevAdminPassword = "Admin123!";
        private const string DevUserPassword = "User123!";

        private static readonly string[] DemoUserEmails =
        {
            "user@clinic.com",
            "BobB@clinic.com",
            "NickT@clinic.com",
            "SaraS@clinic.com",
            "JohnH@clinic.com"
        };

        public static async Task InitializeAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var context = services.GetRequiredService<ClinicBookingSystemContext>();
            var env = services.GetRequiredService<IWebHostEnvironment>();
            var config = services.GetRequiredService<IConfiguration>();

            // 1. ROLES (required in every environment)
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. ADMIN USER
            string adminEmail = config["Seed:AdminEmail"] ?? "admin@clinic.com";
            string? adminPassword = config["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(adminPassword) && env.IsDevelopment())
            {
                adminPassword = DevAdminPassword;
            }

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                // Outside Development with no configured password: skip rather than fall back to a known password.
                Program.logger.LogWarn("Seed:AdminPassword is not configured; skipping admin account seeding.");
            }
            else
            {
                await EnsureUserAsync(userManager, adminEmail, adminPassword, "Admin");
            }

            // 3. SAMPLE USERS AND APPOINTMENTS (Development only)
            if (!env.IsDevelopment())
            {
                return;
            }

            foreach (var email in DemoUserEmails)
            {
                await EnsureUserAsync(userManager, email, DevUserPassword, "User");
            }

            // 4. SAMPLE APPOINTMENTS
            if (!context.Appointments.Any())
            {
                context.Appointments.AddRange(
                    new Appointment
                    {
                        StartTime = DateTime.Now.AddDays(1),
                        EndTime = DateTime.Now.AddDays(1).AddHours(1),
                        IsAvailable = true
                    },
                    new Appointment
                    {
                        StartTime = DateTime.Now.AddDays(2),
                        EndTime = DateTime.Now.AddDays(2).AddHours(1),
                        IsAvailable = true
                    },
                    new Appointment
                    {
                        StartTime = DateTime.Now.AddDays(2),
                        EndTime = DateTime.Now.AddDays(2).AddHours(1),
                        IsAvailable = true
                    },
                      new Appointment
                      {
                          StartTime = DateTime.Now.AddDays(1),
                          EndTime = DateTime.Now.AddDays(1).AddHours(1),
                          IsAvailable = true
                      },
                    new Appointment
                    {
                        StartTime = DateTime.Now.AddDays(2),
                        EndTime = DateTime.Now.AddDays(2).AddHours(1),
                        IsAvailable = true
                    },
                    new Appointment
                    {
                        StartTime = DateTime.Now.AddDays(2),
                        EndTime = DateTime.Now.AddDays(2).AddHours(1),
                        IsAvailable = true
                    }
                );
                await context.SaveChangesAsync();
            }
        }

        // Creates the user (if missing) and assigns the role. Identity errors are logged instead of being silently ignored.
        private static async Task EnsureUserAsync(UserManager<AppUser> userManager, string email, string password, string role)
        {
            if (await userManager.FindByEmailAsync(email) != null)
            {
                return;
            }

            var user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                Program.logger.LogError($"Failed to seed user {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
                return;
            }

            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                Program.logger.LogError($"Failed to add seeded user {email} to role {role}: {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}
