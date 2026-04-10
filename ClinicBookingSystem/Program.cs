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
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<ClinicBookingSystemContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ClinicBookingSystemContext") ?? throw new InvalidOperationException("Connection string 'ClinicBookingSystemContext' not found.")));

            builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
            }) // Configure Identity to use the custom AppUser and the default IdentityRole
                            .AddEntityFrameworkStores<ClinicBookingSystemContext>()
                            .AddDefaultTokenProviders();

            // Add services to the container.
            // Provide a simple No-op email sender so pages that depend on IEmailSender can be activated.
            builder.Services.AddSingleton<IEmailSender, NoOpEmailSender>();
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages(); // Add Razor Pages for Identity UI (login, register, etc.)

            var app = builder.Build();

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
