using ClinicBookingSystem.Data;
using ClinicBookingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace ClinicBookingSystem
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<ClinicBookingSystemContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ClinicBookingSystemContext") ?? throw new InvalidOperationException("Connection string 'ClinicBookingSystemContext' not found.")));

            builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true; 
            })  .AddEntityFrameworkStores<ClinicBookingSystemContext>()
                .AddDefaultTokenProviders(); // Configure Identity to use the custom AppUser and the default IdentityRole

            // Add services to the container.
            // Provide a simple No-op email sender so pages that depend on IEmailSender can be activated.
            builder.Services.AddSingleton<IEmailSender, NoOpEmailSender>();
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages(); // Add Razor Pages for Identity UI (login, register, etc.)
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login"; // Set the login path for unauthenticated users
                options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // Set the access denied path for unauthorized users
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                await IdentitySeeder.SeedRolesAsync(services);
                await IdentitySeeder.SeedAdminAsync(services);
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            // Authentication must run before Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.MapRazorPages(); // Map Razor Pages for Identity UI
            app.Run();
        }
    }
}
