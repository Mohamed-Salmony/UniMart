using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Data;
using UniMart_App.Models;
using UniMart_App.Services;
using Microsoft.AspNetCore.Http;

namespace UniMart_App.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FacultyManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ImageStorageService _imageStorageService;
        private readonly string _facultyImagesFolder = "faculty_images";

        public FacultyManagementController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ImageStorageService imageStorageService)
        {
            _context = context;
            _userManager = userManager;
            _imageStorageService = imageStorageService;
        }

        // GET: FacultyManagement
        public async Task<IActionResult> Index()
        {
            var faculties = await _context.Faculties
                .Include(f => f.Products)
                .Include(f => f.Users)
                .ToListAsync();
            return View(faculties);
        }

        // GET: FacultyManagement/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: FacultyManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Faculty faculty, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null)
                {
                    try
                    {
                        faculty.ImageUrl = await _imageStorageService.SaveImageAsync(ImageFile, _facultyImagesFolder);
                    }
                    catch (ArgumentException)
                    {
                        ModelState.AddModelError("ImageFile", "The uploaded file is not a valid image.");
                        return View(faculty);
                    }
                }

                _context.Faculties.Add(faculty);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Faculty created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(faculty);
        }

        // GET: FacultyManagement/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null)
            {
                return NotFound();
            }

            return View(faculty);
        }

        // POST: FacultyManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Faculty faculty, IFormFile? ImageFile)
        {
            if (id != faculty.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingFaculty = await _context.Faculties
                        .FirstOrDefaultAsync(f => f.Id == id);

                    if (existingFaculty == null)
                    {
                        return NotFound();
                    }

                    // Handle image upload if a new image is provided
                    if (ImageFile != null)
                    {
                        try
                        {
                            faculty.ImageUrl = await _imageStorageService.SaveImageAsync(ImageFile, _facultyImagesFolder);
                        }
                        catch (ArgumentException)
                        {
                            ModelState.AddModelError("ImageFile", "The uploaded file is not a valid image.");
                            return View(faculty);
                        }
                    }

                    existingFaculty.Name = faculty.Name;
                    existingFaculty.Description = faculty.Description;
                    existingFaculty.MaxAcademicYears = faculty.MaxAcademicYears;
                    
                    if (faculty.ImageUrl != null)
                    {
                        existingFaculty.ImageUrl = faculty.ImageUrl;
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Faculty updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FacultyExists(faculty.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(faculty);
        }

        // POST: FacultyManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var faculty = await _context.Faculties
                .Include(f => f.Products)
                .Include(f => f.Users)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (faculty == null)
            {
                TempData["ErrorMessage"] = "Faculty not found.";
                return RedirectToAction(nameof(Index));
            }

            if (faculty.Products.Any() || faculty.Users.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete faculty that has associated products or users.";
                return RedirectToAction(nameof(Index));
            }

            _context.Faculties.Remove(faculty);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Faculty deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool FacultyExists(int id)
        {
            return _context.Faculties.Any(e => e.Id == id);
        }
    }
}

