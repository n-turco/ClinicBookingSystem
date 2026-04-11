using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicBookingSystem.Data;
using ClinicBookingSystem.Models;

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
            ModelState.AddModelError("", "Invalid file");
            return View();
        }

        // SIZE LIMIT (5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError("", "File too large");
            return View();
        }

        // ALLOWED TYPES
        var allowedTypes = new[] { "image/jpeg", "image/png", "application/pdf", "text/plain", "text/csv",
                                   "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };

        if (!allowedTypes.Contains(file.ContentType))
        {
            ModelState.AddModelError("", "Invalid file type");
            return View();
        }

        var webRoot = _env.WebRootPath ?? throw new InvalidOperationException("WebRootPath is not configured");
        var uploadsFolder = Path.Combine(webRoot, "uploads");

        // ensure folder exists
        Directory.CreateDirectory(uploadsFolder);

        // sanitize original filename fallback if null/empty
        var originalFileName = Path.GetFileName(file.FileName ?? Guid.NewGuid().ToString());

        var uniqueFileName = Guid.NewGuid() + "_" + originalFileName;

        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        // Save file metadata to database
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
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

        return RedirectToAction("MyUploads");
    }

    // View user uploads
    public IActionResult MyUploads()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized();
        }
        var userId = userIdClaim.Value;

        var files = _context.UploadedFiles
            .Where(f => f.UserId == userId)
            .ToList();

        return View(files);
    }
}