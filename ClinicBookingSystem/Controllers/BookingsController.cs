/*  
 FILE          : BookingsController.cs 
 PROJECT       : SECU2000 - Project
 PROGRAMMER    : Nick Turco | 9056530
 FIRST VERSION : 2026-04-12
 DESCRIPTION   : This file contains the BookingsController class, which is responsible for handling booking-related actions in the Clinic Booking System. 
                 The controller includes actions for viewing bookings (with different views for users and admins), creating new bookings 
                 for available appointments, and deleting bookings (admin only). It uses ASP.NET Core MVC and is protected by role-based 
                 authorization, allowing users to view and manage their own bookings while giving administrators access to all bookings. 
                 The controller interacts with the database context to retrieve and manipulate booking data, ensuring that appointments are 
                 marked as unavailable once booked to prevent double booking.
*/
using ClinicBookingSystem.Data;
using ClinicBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicBookingSystem.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ClinicBookingSystemContext _context; // Database context for accessing booking and appointment data
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
        public async Task<IActionResult> Index()        // Both users and admins can access this, but content differs based on role
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid(); // or return Challenge(); depending on desired behavior
            }

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
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            var booking = await _context.Bookings
                .Include(b => b.Appointment)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            // SECURITY: Users can only edit their own bookings unless admin
            if (!User.IsInRole("Admin") && booking.UserId != user.Id)
            {
                return Forbid();
            }

            await PopulateAppointmentOptionsAsync(booking.AppointmentId);
            return View(booking);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        // SECURITY: Only Id and AppointmentId are bound. UserId/CreatedAt can never be changed from the client.
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppointmentId")] Booking updatedBooking)
        {
            if (id != updatedBooking.Id)
            {
                return BadRequest();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            var booking = await _context.Bookings
                .Include(b => b.Appointment)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            // SECURITY: ownership enforcement
            if (!User.IsInRole("Admin") && booking.UserId != user.Id)
            {
                return Forbid();
            }

            // Nothing to do if the appointment wasn't changed.
            if (booking.AppointmentId == updatedBooking.AppointmentId)
            {
                return RedirectToAction(nameof(Index));
            }

            // SECURITY: The target appointment must exist, be free and be in the future. Without this check a
            // user could move onto a slot someone else already booked (double booking / slot hijacking).
            var newAppointment = await _context.Appointments.FindAsync(updatedBooking.AppointmentId);
            if (newAppointment == null || !newAppointment.IsAvailable || newAppointment.StartTime <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(Booking.AppointmentId), "The selected appointment is not available.");
                await PopulateAppointmentOptionsAsync(booking.AppointmentId);
                return View(booking);
            }

            // Swap the slots: release the old appointment and reserve the new one. Both changes are saved in a
            // single SaveChangesAsync call, which EF Core wraps in one transaction.
            if (booking.Appointment != null)
            {
                booking.Appointment.IsAvailable = true;
            }
            newAppointment.IsAvailable = false;
            booking.AppointmentId = newAppointment.Id;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Appointment)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            // SECURITY: ownership enforcement (previously any logged-in user could view/delete any booking by ID).
            if (!await CanModifyAsync(booking)) return Forbid();

            return View(booking);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken] // Prevent CSRF attacks
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Appointment)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking != null)
            {
                // SECURITY: ownership enforcement on the state-changing request as well as the confirmation page.
                if (!await CanModifyAsync(booking)) return Forbid();

                // Release the slot so it can be booked again; previously it stayed unavailable forever.
                if (booking.Appointment != null)
                {
                    booking.Appointment.IsAvailable = true;
                }

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Returns true when the current user owns the booking or is an admin.
        private async Task<bool> CanModifyAsync(Booking booking)
        {
            if (User.IsInRole("Admin")) return true;

            var user = await _userManager.GetUserAsync(User);
            return user != null && booking.UserId == user.Id;
        }

        // Builds the appointment dropdown for the Edit view: the booking's current slot plus every free future slot.
        private async Task PopulateAppointmentOptionsAsync(int currentAppointmentId)
        {
            var options = await _context.Appointments
                .Where(a => a.Id == currentAppointmentId || (a.IsAvailable && a.StartTime > DateTime.Now))
                .OrderBy(a => a.StartTime)
                .Select(a => new { a.Id, a.StartTime, a.EndTime })
                .ToListAsync();

            ViewBag.AppointmentId = new SelectList(
                options.Select(a => new { a.Id, Label = $"{a.StartTime:g} - {a.EndTime:t}" }),
                "Id", "Label", currentAppointmentId);
        }
        // USER: View their own bookings
        // ADMIN: View all bookings
        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

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