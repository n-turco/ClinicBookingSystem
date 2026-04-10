using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicBookingSystem.Models
{
    public class UploadedFile
    {
        public int Id { get; set; }

        [Required]
        public string? FileName { get; set; }

        [Required]
        public string? FilePath { get; set; }

        [Required]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Owner
        [Required]
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public AppUser? User { get; set; }
    }
}
