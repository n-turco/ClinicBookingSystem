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

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ClinicBookingSystemContext _context;
    private readonly UserManager<AppUser> _userManager;

    public AdminController(
        ClinicBookingSystemContext context,
        UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;     // Inject the database context and user manager through the constructor
    }
    [Authorize(Roles = "Admin")]        // Ensure that only users with the "Admin" role can access the Dashboard action
    public IActionResult Dashboard()
    {
        var model = new AdminDashboardViewModel     // Create a view model to hold the dashboard data
        {
            TotalBookings = _context.Bookings.Count(),
            TotalAppointments = _context.Appointments.Count(),
            TotalUsers = _userManager.Users.Count(),
            TotalUploads = _context.UploadedFiles.Count()
        };

        return View(model);
    }

    [Authorize(Roles = "Admin")]        
    public IActionResult Users()
    {
        var users = _userManager.Users.ToList();
        return View(users);
    }
}