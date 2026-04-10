using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ClinicBookingSystem.Services
{
    // Simple no-op email sender for development.
    public class NoOpEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Intentionally do nothing 
            return Task.CompletedTask;
        }
    }
}
