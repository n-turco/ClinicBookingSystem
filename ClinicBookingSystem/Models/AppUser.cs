using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ClinicBookingSystem.Models
{
    public class AppUser : IdentityUser // Inherit from IdentityUser to include built-in properties like Id, UserName, Email, etc. (authentication/authorization)
    {
        public string? FullName { get; set; }

        // Navigation
        public ICollection<Booking>? Bookings { get; set; }  // A user can have multiple bookings
        public ICollection<UploadedFile>? UploadedFiles { get; set; } // A user can upload multiple files
    }
}
