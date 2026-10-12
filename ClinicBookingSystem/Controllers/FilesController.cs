/*
 FILE          : FilesController.cs
 PROJECT       : SECU2000 - Project
 PROGRAMMER    : Nick Turco | 9056530
 FIRST VERSION : 2026-04-12
 DESCRIPTION   : This file contains the FilesController class, which is responsible for handling file upload and management actions in the Clinic Booking System.
                 The controller includes actions for uploading files (with validation for file size and type) and viewing a user's uploaded files.
                 It uses ASP.NET Core MVC and is protected by authorization, allowing only authenticated users to access its actions.
                 The controller interacts with the database context to save metadata about uploaded files, including the file name, path, content type, upload time, and associated user ID.
                 Uploaded files are stored in a designated "UploadedFiles" folder located relative to the application's content root (outside the web root).
                 The runtime path used is: Path.Combine(env.ContentRootPath, "UploadedFiles")
*/
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicBookingSystem.Data;
using ClinicBookingSystem.Models;
using ClinicBookingSystem;

// All actions require a logged-in user; viewing a file also checks ownership.
[Authorize]
public class FilesController : Controller
{
    private readonly ClinicBookingSystemContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FilesController> _logger;

    // Inject the database context, hosting environment (for the content root path) and logger through the constructor
    public FilesController(ClinicBookingSystemContext context, IWebHostEnvironment env, ILogger<FilesController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    // GET: Files/Upload
    // USER: Show the upload form
    public IActionResult Upload()
    {
        return View();
    }

    // POST: Files/Upload
    // USER: Validate and store an uploaded file
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        // Reject a missing or empty file
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Upload rejected: empty file.");
            ModelState.AddModelError("", "Invalid file");
            return View();
        }

        // SIZE LIMIT (5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            _logger.LogWarning("Upload rejected: file too large ({FileSize} bytes).", file.Length);
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

        // NOTE: ContentType is sent by the client and can be spoofed. File signatures (magic bytes) are not checked yet.
        if (!allowedTypes.Contains(file.ContentType))
        {
            _logger.LogWarning("Upload rejected: content type not allowed.");
            ModelState.AddModelError("", "Invalid file type");
            return View();
        }

        // Save uploads to the designated uploads folder relative to the application's content root (outside web root)
        var contentRoot = _env.ContentRootPath ?? throw new InvalidOperationException("ContentRootPath is not configured");
        var uploadsFolder = Path.Combine(contentRoot, "UploadedFiles");
        // Create the folder if it does not exist yet (no-op when it already does)
        Directory.CreateDirectory(uploadsFolder);

        // SECURITY: Path.GetFileName strips any directory part from the client-supplied name (blocks "..\" traversal)
        var originalFileName = Path.GetFileName(file.FileName ?? Guid.NewGuid().ToString());

        var uniqueFileName = Guid.NewGuid() + "_" + originalFileName;      // prepend GUID to ensure uniqueness and prevent overwriting

        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // Write the uploaded bytes to disk
        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
            _logger.LogInformation("Saved uploaded file as {StoredFileName}.", uniqueFileName);
        }
        // Save file metadata to database
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            _logger.LogError("Authenticated user has no NameIdentifier claim; cannot associate uploaded file with a user.");
            // no user id claim unauthorized
            return Unauthorized();
        }
        var userId = userIdClaim.Value;

        // SECURITY: The owner comes from the auth cookie, not from form data.
        UploadedFile uploadedFile = new UploadedFile
        {
            FileName = file.FileName,
            // Store the relative path (to content root) for portability. Serving should be done via an authenticated endpoint.
            FilePath = Path.Combine("UploadedFiles", uniqueFileName),
            ContentType = file.ContentType,
            UploadedAt = DateTime.UtcNow,
            UserId = userId
        };

        _context.UploadedFiles.Add(uploadedFile);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved metadata for file {FileId} owned by user {UserId}.", uploadedFile.Id, userId);

        return RedirectToAction("MyUploads");
    }

    // GET: Files/MyUploads
    // USER: View user uploads
    [Authorize(Roles = "User")]
    public IActionResult MyUploads()
    {
        // Identify the current user from their claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            _logger.LogError("Authenticated user has no NameIdentifier claim; cannot list uploaded files.");
            return Unauthorized();
        }
        var userId = userIdClaim.Value;

        // Only list files owned by the current user
        var files = _context.UploadedFiles
            .Where(f => f.UserId == userId)
            .ToList();

        return View(files);
    }

    // GET: /Files/ViewDocument/{id}
    // Streams the requested file to the authenticated user after authorization check.
    [HttpGet]
    public async Task<IActionResult> ViewDocument(int id)
    {
        // Look up the file's metadata record
        var uploadedFile = await _context.UploadedFiles.FindAsync(id);
        if (uploadedFile == null)
        {
            _logger.LogWarning("ViewDocument requested for non-existent file {FileId}.", id);
            return NotFound();
        }

        // Verify ownership or that the user is in an allowed role (e.g., Admin)
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            _logger.LogError("Authenticated user has no NameIdentifier claim; file view denied.");
            return Unauthorized();
        }
        var userId = userIdClaim.Value;
        if (uploadedFile.UserId != userId && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("User {UserId} attempted to view file {FileId} owned by {OwnerId}.", userId, uploadedFile.Id, uploadedFile.UserId);
            return Forbid();
        }

        // SECURITY: Never trust the stored FilePath as a path. Path.Combine discards the root when given an
        // absolute path ("C:\...") and does not stop "..\" traversal, so a tampered record could read any file
        // on the server. Only the file-name component is used, and the resolved path must stay inside the
        // uploads folder.
        var uploadsRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "UploadedFiles"));
        var storedFileName = Path.GetFileName(uploadedFile.FilePath ?? string.Empty);
        var physicalPath = Path.GetFullPath(Path.Combine(uploadsRoot, storedFileName));

        if (string.IsNullOrEmpty(storedFileName) ||
            !physicalPath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Blocked file access outside the uploads folder for file {FileId}.", uploadedFile.Id);
            return NotFound();
        }

        // The metadata exists but the file itself is missing from disk
        if (!System.IO.File.Exists(physicalPath))
        {
            _logger.LogWarning("File {FileId} not found on disk.", uploadedFile.Id);
            return NotFound();
        }

        // Content types that browsers can render inline
        var inlineContentTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/gif", "text/plain" };

        _logger.LogInformation("User {UserId} is viewing file {FileId}.", userId, uploadedFile.Id);

        // The stream is disposed by the FileStreamResult once the response is sent
        var stream = System.IO.File.OpenRead(physicalPath);
        var contentType = uploadedFile.ContentType ?? "application/octet-stream";

        if (inlineContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            // Tell browser to display inline
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{uploadedFile.FileName}\"";
        }
        else
        {
            // Anything else is downloaded rather than rendered
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{uploadedFile.FileName}\"";
        }

        return File(stream, contentType);
    }
}
