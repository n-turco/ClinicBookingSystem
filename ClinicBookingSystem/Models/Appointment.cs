using System.ComponentModel.DataAnnotations;

namespace ClinicBookingSystem.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Navigation
        public ICollection<Booking>? Bookings { get; set; }

    }
}
