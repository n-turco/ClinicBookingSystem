/*  
 FILE          : FilesController.cs 
 PROJECT       : SECU2000 - Project
 PROGRAMMER    : Nick Turco | 9056530
 FIRST VERSION : 2026-04-12
 DESCRIPTION   : This file contains the FilesController class, which is responsible for handling file upload and management actions in the Clinic Booking System. 
                 The controller includes actions for uploading files (with validation for file size and type) and viewing a user's uploaded files. 
                 It uses ASP.NET Core MVC and is protected by authorization, allowing only authenticated users to access its actions. 
                 The controller interacts with the database context to save metadata about uploaded files, including the file name, path, content type, upload time, and associated user ID. 
                 Uploaded files are stored in a designated "uploads" folder within the web root directory of the application.
*/
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicBookingSystem.Data;
using ClinicBookingSystem.Models;
using ClinicBookingSystem;

[Authorize]
public class FilesController : Controller
{
    private readonly ClinicBookingSystemContext _context;
    private readonly IWebHostEnvironment _env;

    public FilesController(ClinicBookingSystemContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET
    public IActionResult Upload()
    {
        return View();
    }

    // POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            Program.logger.LogWarn("User attempted to upload an empty file.");
            ModelState.AddModelError("", "Invalid file");
            return View();
        }

        // SIZE LIMIT (5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            Program.logger.LogWarn($"User attempted to upload a file that is too large: {file.FileName} ({file.Length} bytes).");
            ModelState.AddModelError("", "File too large");
            return View();
        }

        // ALLOWED TYPES
        var allowedTypes = new[]
         {
            "image/jpeg",
            "image/png",
            "application/pdf",
            "text/plain",
            "text/csv",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/json"
        };

        if (!allowedTypes.Contains(file.ContentType))   //MIME type validation is not trusted alone, also checking file signatures.
        {
            Program.logger.LogWarn($"User attempted to upload a file with an invalid content type: {file.FileName} ({file.ContentType}).");
            ModelState.AddModelError("", "Invalid file type");
            return View();
        }

        var webRoot = _env.WebRootPath ?? throw new InvalidOperationException("WebRootPath is not configured");
        var uploadsFolder = Path.Combine(webRoot, "uploads");

        // ensure folder exists
        Directory.CreateDirectory(uploadsFolder);

        // sanitize original filename fallback if null/empty
        var originalFileName = Path.GetFileName(file.FileName ?? Guid.NewGuid().ToString());

        var uniqueFileName = Guid.NewGuid() + "_" + originalFileName;      // prepend GUID to ensure uniqueness and prevent overwriting

        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
            Program.logger.LogInfo($"Saving uploaded file: {file.FileName} as {uniqueFileName} at {filePath}");
        }
        // Save file metadata to database
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            Program.logger.LogError("Authenticated user does not have a NameIdentifier claim. Unable to associate uploaded file with user.");
            // no user id claim unauthorized
            return Unauthorized();
        }
        var userId = userIdClaim.Value;

        UploadedFile uploadedFile = new UploadedFile
        {
            FileName = file.FileName,
            FilePath = "/uploads/" + uniqueFileName,
            ContentType = file.ContentType,
            UploadedAt = DateTime.UtcNow,
            UserId = userId
        };

        _context.UploadedFiles.Add(uploadedFile);
        await _context.SaveChangesAsync();
        Program.logger.LogInfo($"File metadata saved to database for file: {file.FileName} (ID: {uploadedFile.Id}) associated with user ID: {userId}.");    

        return RedirectToAction("MyUploads");
    }

    // View user uploads
    public IActionResult MyUploads()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            Program.logger.LogError("Authenticated user does not have a NameIdentifier claim. Unable to retrieve uploaded files for user.");
            return Unauthorized();
        }
        var userId = userIdClaim.Value;

        var files = _context.UploadedFiles
            .Where(f => f.UserId == userId)
            .ToList();

        return View(files);
    }
}