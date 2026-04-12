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

namespace ClinicBookingSystem.Controllers
{
    public class UploadedFilesController : Controller
    {
        private readonly ClinicBookingSystemContext _context;

        public UploadedFilesController(ClinicBookingSystemContext context)
        {
            _context = context;
        }

        // GET: UploadedFiles
        public async Task<IActionResult> Index()
        {
            var clinicBookingSystemContext = _context.UploadedFiles.Include(u => u.User);
            return View(await clinicBookingSystemContext.ToListAsync());
        }

        // GET: UploadedFiles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var uploadedFile = await _context.UploadedFiles
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (uploadedFile == null)
            {
                return NotFound();
            }

            return View(uploadedFile);
        }

        // GET: UploadedFiles/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Set<AppUser>(), "Id", "Id");
            return View();
        }

        // POST: UploadedFiles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FileName,FilePath,ContentType,FileSize,UploadedAt,UserId")] UploadedFile uploadedFile)
        {
            if (ModelState.IsValid)
            {
                _context.Add(uploadedFile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Set<AppUser>(), "Id", "Id", uploadedFile.UserId);
            return View(uploadedFile);
        }

        // GET: UploadedFiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var uploadedFile = await _context.UploadedFiles.FindAsync(id);
            if (uploadedFile == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Set<AppUser>(), "Id", "Id", uploadedFile.UserId);
            return View(uploadedFile);
        }

        // POST: UploadedFiles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FileName,FilePath,ContentType,FileSize,UploadedAt,UserId")] UploadedFile uploadedFile)
        {
            if (id != uploadedFile.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(uploadedFile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UploadedFileExists(uploadedFile.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Set<AppUser>(), "Id", "Id", uploadedFile.UserId);
            return View(uploadedFile);
        }

        // GET: UploadedFiles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var uploadedFile = await _context.UploadedFiles
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (uploadedFile == null)
            {
                return NotFound();
            }

            return View(uploadedFile);
        }

        // POST: UploadedFiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var uploadedFile = await _context.UploadedFiles.FindAsync(id);
            if (uploadedFile != null)
            {
                _context.UploadedFiles.Remove(uploadedFile);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UploadedFileExists(int id)
        {
            return _context.UploadedFiles.Any(e => e.Id == id);
        }
    }
}
