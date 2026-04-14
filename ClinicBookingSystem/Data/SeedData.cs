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
*/
using Microsoft.AspNetCore.Identity;
using ClinicBookingSystem.Models;

namespace ClinicBookingSystem.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var context = services.GetRequiredService<ClinicBookingSystemContext>();

            // 1. ROLES
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. ADMIN USER
            string adminEmail = "admin@clinic.com";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // 3. SAMPLE USER
            string userEmail = "user@clinic.com";

            if (await userManager.FindByEmailAsync(userEmail) == null)
            {
                var user = new AppUser
                {
                    UserName = userEmail,
                    Email = userEmail,
                    EmailConfirmed = true
                };
                
                await userManager.CreateAsync(user, "User123!");
                await userManager.AddToRoleAsync(user, "User");
            }

            string user1Email = "BobB@clinic.com";

            if (await userManager.FindByEmailAsync(user1Email) == null)
            {
                var user = new AppUser
                {
                    UserName = user1Email,
                    Email = user1Email,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, "User123!");
                await userManager.AddToRoleAsync(user, "User");
            }
            string user2Email = "NickT@clinic.com";

            if (await userManager.FindByEmailAsync(user2Email) == null)
            {
                var user = new AppUser
                {
                    UserName = user2Email,
                    Email = user2Email,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, "User123!");
                await userManager.AddToRoleAsync(user, "User");
            }
            string user3Email = "SaraS@clinic.com";

            if (await userManager.FindByEmailAsync(user3Email) == null)
            {
                var user = new AppUser
                {
                    UserName = user3Email,
                    Email = user3Email,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, "User123!");
                await userManager.AddToRoleAsync(user, "User");
            }
            string user4Email = "JohnH@clinic.com";

            if (await userManager.FindByEmailAsync(user4Email) == null)
            {
                var user = new AppUser
                {
                    UserName = user4Email,
                    Email = user4Email,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, "User123!");
                await userManager.AddToRoleAsync(user, "User");
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
    }
}