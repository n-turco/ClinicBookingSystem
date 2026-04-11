using ClinicBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ClinicBookingSystem.Controllers
{
    [Authorize] // Ensure that only authenticated users can access the HomeController
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
          //  return Redirect("/Identity/Account/Login"); // Redirect to the login page as the default landing page
           return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
