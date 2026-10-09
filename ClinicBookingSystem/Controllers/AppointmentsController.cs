/*
 FILE          : AppointmentsController.cs
 PROJECT       : SECU2000 - Project
 PROGRAMMER    : Nick Turco | 9056530
 FIRST VERSION : 2026-04-12
 DESCRIPTION   : This file contains the AppointmentsController class, which is responsible for handling appointment-related
                 actions in the Clinic Booking System. The controller includes actions for viewing available appointments,
                 creating new appointments (admin only), editing and deleting appointments (admin only), and searching for
                 appointments based on date criteria. It uses ASP.NET Core MVC and is protected by role-based authorization,
                 allowing only users with the "Admin" role to access certain actions. The controller interacts with the database
                 context to retrieve and manipulate appointment data.
*/
using ClinicBookingSystem.Data;
using ClinicBookingSystem.Models;
using ClinicBookingSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicBookingSystem.Controllers
{
    // All actions require a logged-in user; create/edit/delete are further restricted to admins per action.
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ClinicBookingSystemContext _context;

        // Inject the database context through the constructor
        public AppointmentsController(ClinicBookingSystemContext context)
        {
            _context = context;
        }

        // GET: Appointments
        // USER: View available appointments
        public async Task<IActionResult> Index()
        {
            // Only list slots that have not been booked yet
            var appointments = await _context.Appointments
                .Where(a => a.IsAvailable)
                .ToListAsync();

            return View(appointments);
        }

        // GET: Appointments/Create
        // ADMIN: Create appointment (GET)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }


        // GET: Appointments/Search
        // USER: Show the empty search form
        [Authorize]
        public IActionResult Search()
        {
            return View(new AppointmentSearchModel());
        }

        // POST: Appointments/Create
        // ADMIN: Create appointment (POST)
        // SECURITY: Only StartTime and EndTime are bound; availability is set server-side (prevents overposting).
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StartTime,EndTime")] Appointment appointment)
        {
            // Check the appointment ends after it starts
            if (appointment.EndTime <= appointment.StartTime)
            {
                ModelState.AddModelError("", "End time must be after start time.");
            }

            if (ModelState.IsValid)
            {
                // New appointments always start out open for booking
                appointment.IsAvailable = true;

                // Save the appointment and return to the list
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // Validation failed, redisplay the form with errors
            return View(appointment);
        }

        // POST: Appointments/Search
        // USER: Filter available appointments by an optional date range
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(AppointmentSearchModel model)
        {
            // Start from available appointments only
            var query = _context.Appointments
                .Where(a => a.IsAvailable);

            // Apply the start date filter if one was entered
            if (model.StartDate.HasValue)
            {
                query = query.Where(a => a.StartTime >= model.StartDate.Value);
            }

            // Apply the end date filter if one was entered
            if (model.EndDate.HasValue)
            {
                query = query.Where(a => a.EndTime <= model.EndDate.Value);
            }

            // Filters are applied through LINQ, so EF Core sends them as parameterized SQL
            var results = await query.ToListAsync();

            return View("SearchResults", results);
        }

        // GET: Appointments/Edit/5
        // ADMIN: Edit appointment
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            // Check if an ID was provided
            if (id == null) return NotFound();

            // Check if appointment exists
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // POST: Appointments/Edit/5
        // ADMIN: Edit appointment (POST)
        // SECURITY: Only the times are bound, so IsAvailable cannot be changed through the edit form.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StartTime,EndTime")] Appointment appointment)
        {
            // Check the route ID matches the ID posted in the form body
            if (id != appointment.Id) return NotFound();

            // Check the appointment ends after it starts
            if (appointment.EndTime <= appointment.StartTime)
            {
                ModelState.AddModelError("", "End time must be after start time.");
            }

            if (ModelState.IsValid)
            {
                // Check if appointment exists
                var existing = await _context.Appointments.FindAsync(id);
                if (existing == null) return NotFound();

                // Copy only the editable fields onto the tracked entity
                existing.StartTime = appointment.StartTime;
                existing.EndTime = appointment.EndTime;

                // Save the changes and return to the list
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Validation failed, redisplay the form with errors
            return View(appointment);
        }

        // GET: Appointments/Delete/5
        // ADMIN: Delete appointment
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            // Check if an ID was provided
            if (id == null) return NotFound();

            // Look up the appointment for the confirmation page
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(m => m.Id == id);

            // Check if appointment exists
            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        // ADMIN: Delete appointment (POST)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            // Remove the appointment if it still exists (it may already have been deleted)
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

    }
}