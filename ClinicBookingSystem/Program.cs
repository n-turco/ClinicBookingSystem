using ClinicBookingSystem.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ClinicBookingSystem.Models;
namespace ClinicBookingSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<ClinicBookingSystemContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ClinicBookingSystemContext") ?? throw new InvalidOperationException("Connection string 'ClinicBookingSystemContext' not found.")));

            builder.Services.AddIdentity<AppUser, IdentityRole>() // Configure Identity to use the custom AppUser and the default IdentityRole
                            .AddEntityFrameworkStores<ClinicBookingSystemContext>()
                            .AddDefaultTokenProviders();

            // Add services to the container.
            builder.Services.AddControllersWithViews();

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

            app.UseAuthorization();
            app.UseAuthentication();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
