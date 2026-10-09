/*
 FILE          : HomeController.cs
 PROJECT       : SECU2000 - Project
 PROGRAMMER    : Nick Turco | 9056530
 FIRST VERSION : 2026-04-12
 DESCRIPTION   : This file contains the HomeController class, which is responsible for handling the home page and privacy policy of the Clinic Booking System.
                 The controller includes actions for displaying the home page (which serves as the default landing page) and the privacy policy.
                 It uses ASP.NET Core MVC and is protected by authorization, ensuring that only authenticated users can access its actions.
                 The controller also includes an error action that returns an error view with details about any exceptions that occur during request processing.
*/
using ClinicBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ClinicBookingSystem.Controllers
{
    [Authorize] // Ensure that only authenticated users can access the HomeController
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        // Inject the logger through the constructor
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // GET: Home/Index
        // Default landing page shown after login
        public IActionResult Index()
        {
            return View();
        }

        // GET: Home/Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: Home/Error
        // Target of UseExceptionHandler. Caching is disabled so an error page is never served from a cache.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Log the actual exception captured by UseExceptionHandler, rather than a generic message.
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature?.Error != null)
            {
                _logger.LogError(exceptionFeature.Error, "Unhandled exception while processing {Path}.", exceptionFeature.Path);
            }

            // SECURITY: Only a request ID is passed to the view, so exception details are never shown to the user.
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
