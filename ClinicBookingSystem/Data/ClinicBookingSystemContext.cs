using ClinicBookingSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicBookingSystem.Data
{
    public class ClinicBookingSystemContext : IdentityDbContext<AppUser> // Inherit from IdentityDbContext to include ASP.NET Core Identity tables
    {
        // Constructor that accepts DbContextOptions and passes them to the base class constructor
        public ClinicBookingSystemContext(DbContextOptions<ClinicBookingSystemContext> options) : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<UploadedFile> UploadedFiles { get; set; }
    }
}
