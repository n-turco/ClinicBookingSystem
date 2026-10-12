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
    // All actions require a logged-in user; ownership checks inside each action decide what they can see or change.
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ClinicBookingSystemContext _context; // Database context for accessing booking and appointment data
        private readonly UserManager<AppUser> _userManager;

        // Inject the database context and Identity user manager through the constructor
        public BookingsController(
            ClinicBookingSystemContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Bookings
        // ADMIN: View all bookings
        // USER: View only their bookings
        public async Task<IActionResult> Index()        // Both users and admins can access this, but content differs based on role
        {
            // Look up the current user from the auth cookie
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid(); // or return Challenge(); depending on desired behavior
            }

            // Admins see every booking along with the user who made it
            if (User.IsInRole("Admin"))
            {
                var allBookings = _context.Bookings
                    .Include(b => b.Appointment)
                    .Include(b => b.User);

                return View(await allBookings.ToListAsync());
            }

            // Regular users only see bookings they own
            var userBookings = _context.Bookings
                .Where(b => b.UserId == user.Id)
                .Include(b => b.Appointment);

            return View(await userBookings.ToListAsync());
        }


        // GET: Bookings/Create/5
        // USER: Create booking for an available appointment
        public async Task<IActionResult> Create(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            // Only show the confirmation page for slots that exist and are still open
            if (appointment == null || !appointment.IsAvailable)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // POST: Bookings/CreateConfirmed/5
        // USER: Confirm booking creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConfirmed(int id)
        {
            // Re-check availability on the POST, since the slot may have been taken after the GET page loaded
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null || !appointment.IsAvailable)
            {
                return BadRequest("Appointment not available");
            }

            var user = await _userManager.GetUserAsync(User);

            // SECURITY: The owner and timestamp come from the server, not from form data.
            var booking = new Booking
            {
                AppointmentId = id,
                UserId = user?.Id,
                CreatedAt = DateTime.UtcNow
            };

            // Prevent double booking
            appointment.IsAvailable = false;

            // Save the booking and the appointment change together
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Bookings/Edit/5
        // USER: Move their own booking to a different slot
        // ADMIN: Move any booking
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            // Load the booking with its current appointment
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

            // Fill the appointment dropdown before showing the form
            await PopulateAppointmentOptionsAsync(booking.AppointmentId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // SECURITY: Only Id and AppointmentId are bound. UserId/CreatedAt can never be changed from the client.
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppointmentId")] Booking updatedBooking)
        {
            // The ID in the route must match the ID in the form
            if (id != updatedBooking.Id)
            {
                return BadRequest();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            // Load the stored booking; changes are applied to this tracked entity, not the posted one
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

        // GET: Bookings/Delete/5
        // USER: Confirm cancelling their own booking
        // ADMIN: Confirm cancelling any booking
        public async Task<IActionResult> Delete(int? id)
        {
            // Check if an ID was provided
            if (id == null) return NotFound();

            // Load the booking with its appointment and owner for the confirmation page
            var booking = await _context.Bookings
                .Include(b => b.Appointment)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            // SECURITY: ownership enforcement (previously any logged-in user could view/delete any booking by ID).
            if (!await CanModifyAsync(booking)) return Forbid();

            return View(booking);
        }

        // POST: Bookings/Delete/5
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

                // Remove the booking and save the appointment change in the same call
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

            // Build readable labels such as "4/12/2026 9:00 AM - 9:30 AM", with the current slot preselected
            ViewBag.AppointmentId = new SelectList(
                options.Select(a => new { a.Id, Label = $"{a.StartTime:g} - {a.EndTime:t}" }),
                "Id", "Label", currentAppointmentId);
        }
        // GET: Bookings/MyBookings
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

            // Admins see every booking along with the user who made it
            if (User.IsInRole("Admin"))
            {
                var allBookings = await _context.Bookings
                    .Include(b => b.Appointment)
                    .Include(b => b.User)
                    .ToListAsync();

                return View(allBookings);
            }

            // Regular users only see bookings they own
            var userBookings = await _context.Bookings
                .Where(b => b.UserId == user.Id)
                .Include(b => b.Appointment)
                .ToListAsync();

            return View(userBookings);
        }
    }
}
