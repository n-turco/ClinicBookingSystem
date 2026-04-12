using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ClinicBookingSystem.Services
{
    // Simple email sender for development, need this to satisfy the IEmailSender dependency for
    // Identity pages that send emails (registration confirmation).
    public class NoOpEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Intentionally do nothing 
            return Task.CompletedTask;
        }
    }
}
