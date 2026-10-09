/*  
 FILE          : UploadedFilesController.cs 
 PROJECT       : SECU2000 - Project
 PROGRAMMER    : Nick Turco | 9056530
 FIRST VERSION : 2026-04-12
 DESCRIPTION   : This file contains the UploadedFilesController class, which is responsible for handling CRUD operations related to uploaded files in the Clinic Booking System. 
                 The controller includes actions for listing all uploaded files, viewing details of a specific file, creating new file records, editing existing file records, and deleting file records. 
                 It uses ASP.NET Core MVC and is protected by authorization, allowing only authenticated users to access its actions. 
                 The controller interacts with the database context to retrieve and manipulate data about uploaded files, including 
                 their metadata such as file name, path, content type, size, upload time, and associated user ID.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicBookingSystem.Data;
using ClinicBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;

namespace ClinicBookingSystem.Controllers
{
    // SECURITY: This controller exposes every user's file metadata, so it is restricted to admins.
    // Previously it had no [Authorize] at all, allowing anonymous users to list, create, edit and delete records.
    [Authorize(Roles = "Admin")]
    public class UploadedFilesController : Controller
    {
        private readonly ClinicBookingSystemContext _context;
        private readonly ILogger<UploadedFilesController> _logger;

        public UploadedFilesController(ClinicBookingSystemContext context, ILogger<UploadedFilesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: UploadedFiles
        public async Task<IActionResult> Index()
        {
            // Load every file record along with the user who uploaded it
            var clinicBookingSystemContext = _context.UploadedFiles.Include(u => u.User);
            return View(await clinicBookingSystemContext.ToListAsync());
        }

        // GET: UploadedFiles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            // Check if an ID was provided
            if (id == null)
            {
                _logger.LogWarning("Uploaded file details requested without an ID.");
                return NotFound();
            }

            // Look up the file record and its uploader
            var uploadedFile = await _context.UploadedFiles
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Check if file exists
            if (uploadedFile == null)
            {
                return NotFound();
            }

            return View(uploadedFile);
        }

        // SECURITY: The scaffolded Create actions were removed. File records must only be created by
        // FilesController.Upload, which writes the physical file and sets FilePath/UserId server-side.
        // Letting a client supply FilePath allowed pointing a record at any file on the server.

        // GET: UploadedFiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            // Check if an ID was provided
            if (id == null)
            {
                return NotFound();
            }

            // Check if file exists
            var uploadedFile = await _context.UploadedFiles.FindAsync(id);
            if (uploadedFile == null)
            {
                _logger.LogWarning("Edit requested for non-existent uploaded file {FileId}.", id);
                return NotFound();
            }
            return View(uploadedFile);
        }

        // POST: UploadedFiles/Edit/5
        // SECURITY: Only the display FileName can be changed. FilePath, ContentType and UserId are server-owned
        // and are never bound from the request (prevents overposting / re-pointing a record at another file).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FileName")] UploadedFile uploadedFile)
        {
            // Check the route ID matches the ID posted in the form body
            if (id != uploadedFile.Id)
            {
                _logger.LogWarning("Uploaded file edit ID mismatch: route {RouteId}, body {BodyId}.", id, uploadedFile.Id);
                return NotFound();
            }

            // Check if file exists
            var existing = await _context.UploadedFiles.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Uploaded file {FileId} was not found during update.", id);
                return NotFound();
            }

            // Check if a file name was entered
            if (string.IsNullOrWhiteSpace(uploadedFile.FileName))
            {
                ModelState.AddModelError(nameof(UploadedFile.FileName), "File name is required.");
                return View(existing);
            }

            // Strip any directory components so the display name can't carry a path.
            existing.FileName = Path.GetFileName(uploadedFile.FileName.Trim());

            // Save the new name and return to the file list
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: UploadedFiles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            // Check if an ID was provided
            if (id == null)
            {
                _logger.LogWarning("Uploaded file delete requested without an ID.");
                return NotFound();
            }

            // Look up the file record and its uploader for the confirmation page
            var uploadedFile = await _context.UploadedFiles
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Check if file exists
            if (uploadedFile == null)
            {
                _logger.LogWarning("Delete requested for non-existent uploaded file {FileId}.", id);
                return NotFound();
            }

            return View(uploadedFile);
        }

        // POST: UploadedFiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Remove the record if it still exists (it may already have been deleted)
            var uploadedFile = await _context.UploadedFiles.FindAsync(id);
            if (uploadedFile != null)
            {
                _context.UploadedFiles.Remove(uploadedFile);
            }

            // Save the change and return to the file list
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
