namespace ClinicBookingSystem.Models
{
    public class AppUser
    {
        public string? FullName { get; set; }

        // Navigation
        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<UploadedFile>? UploadedFiles { get; set; }
    }
}
