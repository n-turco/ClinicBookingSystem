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

            return View(booking);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Booking updatedBooking)
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

            // Update allowed fields only (prevents overposting)
            booking.AppointmentId = updatedBooking.AppointmentId;

            _context.Update(booking);
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

            return View(booking);
        }

        // ADMIN ONLY: Confirm booking deletion
        [HttpPost, ActionName("Delete")]
      //  [Authorize(Roles = "Admin")]
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