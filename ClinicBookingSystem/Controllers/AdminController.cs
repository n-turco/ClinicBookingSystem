/*
 FILE          : AdminController.cs
 PROJECT       : SECU2000 - Project
 PROGRAMMER    : Nick Turco | 9056530
 FIRST VERSION : 2026-04-12
 DESCRIPTION   : This file contains the AdminController class, which is responsible for handling administrative actions
                 in the Clinic Booking System. The controller includes actions for displaying the admin dashboard and managing users.
                 It uses ASP.NET Core MVC and is protected by role-based authorization, allowing only users with the "Admin"
                 role to access its actions. The controller interacts with the database context to retrieve data for the dashboard
                 and user management views.
*/
using ClinicBookingSystem.Data;
using ClinicBookingSystem.Models;
using ClinicBookingSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// SECURITY: Every action in this controller exposes system-wide data, so the whole class is restricted to admins.
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ClinicBookingSystemContext _context;
    private readonly UserManager<AppUser> _userManager;

    // Inject the database context and user manager through the constructor
    public AdminController(
        ClinicBookingSystemContext context,
        UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Admin/Dashboard
    // Ensure that only users with the "Admin" role can access the Dashboard action
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Dashboard()
    {
        // Build the dashboard view model from record counts across the system.
        // The counts are awaited one at a time because a DbContext does not support concurrent queries.
        var model = new AdminDashboardViewModel
        {
            TotalBookings = await _context.Bookings.CountAsync(),
            TotalAppointments = await _context.Appointments.CountAsync(),
            TotalUsers = await _userManager.Users.CountAsync(),
            TotalUploads = await _context.UploadedFiles.CountAsync()
        };

        return View(model);
    }

    // GET: Admin/Users
    // Ensure that only users with the "Admin" role can view the list of registered users
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users()
    {
        // Load every registered user account for the user management view
        var users = await _userManager.Users.ToListAsync();
        return View(users);
    }
}