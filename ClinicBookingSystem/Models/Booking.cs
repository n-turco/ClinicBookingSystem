using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicBookingSystem.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Timestamp for logging when the booking was made (repudiation)

        // Navigation
        [ForeignKey("UserId")]
        public AppUser? User { get; set; }

        [ForeignKey("AppointmentId")]
        public Appointment? Appointment { get; set; }
    }
}
