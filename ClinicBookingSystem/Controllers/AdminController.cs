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
        _userManager = userManager;
    }

    public IActionResult Dashboard()
    {
        var model = new AdminDashboardViewModel
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