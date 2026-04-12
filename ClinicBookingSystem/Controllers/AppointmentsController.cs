/*  
 FILE          : AppointmentController.cs 
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
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ClinicBookingSystemContext _context;

        public AppointmentsController(ClinicBookingSystemContext context)
        {
            _context = context;
        }

        // USER: View available appointments
        public async Task<IActionResult> Index()
        {
            var appointments = await _context.Appointments
                .Where(a => a.IsAvailable)
                .ToListAsync();

            return View(appointments);
        }

        // ADMIN: Create appointment (GET)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }
        

        [Authorize]
        public IActionResult Search()
        {
            return View(new AppointmentSearchModel());
        }

        // ADMIN: Create appointment (POST)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StartTime,EndTime")] Appointment appointment)
        {
            if (appointment.EndTime <= appointment.StartTime)
            {
                ModelState.AddModelError("", "End time must be after start time.");
            }

            if (ModelState.IsValid)
            {
                appointment.IsAvailable = true;

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(appointment);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(AppointmentSearchModel model)
        {
            var query = _context.Appointments
                .Where(a => a.IsAvailable);

            if (model.StartDate.HasValue)
            {
                query = query.Where(a => a.StartTime >= model.StartDate.Value);
            }

            if (model.EndDate.HasValue)
            {
                query = query.Where(a => a.EndTime <= model.EndDate.Value);
            }

            var results = await query.ToListAsync();

            return View("SearchResults", results);
        }

        // ADMIN: Edit appointment
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // ADMIN: Edit appointment (POST)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StartTime,EndTime")] Appointment appointment)
        {
            if (id != appointment.Id) return NotFound();

            if (appointment.EndTime <= appointment.StartTime)
            {
                ModelState.AddModelError("", "End time must be after start time.");
            }

            if (ModelState.IsValid)
            {
                var existing = await _context.Appointments.FindAsync(id);
                if (existing == null) return NotFound();

                existing.StartTime = appointment.StartTime;
                existing.EndTime = appointment.EndTime;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(appointment);
        }

        // ADMIN: Delete appointment
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null) return NotFound();

            return View(appointment);
        }
        
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
      
    }
}