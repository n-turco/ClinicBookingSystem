using ClinicBookingSystem.Data;
using ClinicBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicBookingSystem.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ClinicBookingSystemContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BookingsController(
            ClinicBookingSystemContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ADMIN: View all bookings
        // USER: View only their bookings
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole("Admin"))
            {
                var allBookings = _context.Bookings
                    .Include(b => b.Appointment)
                    .Include(b => b.User);

                return View(await allBookings.ToListAsync());
            }

            var userBookings = _context.Bookings
                .Where(b => b.UserId == user.Id)
                .Include(b => b.Appointment);

            return View(await userBookings.ToListAsync());
        }


        // USER: Create booking for an available appointment
        public async Task<IActionResult> Create(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null || !appointment.IsAvailable)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // USER: Confirm booking creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null || !appointment.IsAvailable)
            {
                return BadRequest("Appointment not available");
            }

            var user = await _userManager.GetUserAsync(User);

            var booking = new Booking
            {
                AppointmentId = id,
                UserId = user?.Id,
                CreatedAt = DateTime.UtcNow
            };

            // Prevent double booking
            appointment.IsAvailable = false;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ADMIN ONLY: Delete booking
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Appointment)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // ADMIN ONLY: Confirm booking deletion
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken] // Prevent CSRF attacks
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        // USER: View their own bookings
        // ADMIN: View all bookings
        [Authorize] // Both users and admins can access this, but content differs based on role
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole("Admin"))
            {
                var allBookings = _context.Bookings
                    .Include(b => b.Appointment)
                    .Include(b => b.User)
                    .ToList();

                return View(allBookings);
            }

            var userBookings = _context.Bookings
                .Where(b => b.UserId == user.Id)
                .Include(b => b.Appointment)
                .ToList();

            return View(userBookings);
        }
    }
}