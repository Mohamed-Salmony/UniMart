using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Data;
using UniMart_App.Models;

namespace UniMart_App.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AcademicYearManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AcademicYearManagementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: AcademicYearManagement
        public async Task<IActionResult> Index()
        {
            var academicYears = await _context.AcademicYears
                .Include(ay => ay.Users)
                .OrderBy(ay => ay.Year)
                .ToListAsync();
            return View(academicYears);
        }

        // GET: AcademicYearManagement/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AcademicYearManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AcademicYear academicYear)
        {
            if (ModelState.IsValid)
            {
                // Check if year already exists
                var existingYear = await _context.AcademicYears
                    .FirstOrDefaultAsync(ay => ay.Year == academicYear.Year);
                
                if (existingYear != null)
                {
                    ModelState.AddModelError("Year", "This academic year already exists.");
                    return View(academicYear);
                }

                _context.AcademicYears.Add(academicYear);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Academic year created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(academicYear);
        }

        // GET: AcademicYearManagement/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var academicYear = await _context.AcademicYears.FindAsync(id);
            if (academicYear == null)
            {
                return NotFound();
            }
            return View(academicYear);
        }

        // POST: AcademicYearManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AcademicYear academicYear)
        {
            if (id != academicYear.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if year already exists (excluding current record)
                    var existingYear = await _context.AcademicYears
                        .FirstOrDefaultAsync(ay => ay.Year == academicYear.Year && ay.Id != id);
                    
                    if (existingYear != null)
                    {
                        ModelState.AddModelError("Year", "This academic year already exists.");
                        return View(academicYear);
                    }

                    _context.Update(academicYear);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Academic year updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AcademicYearExists(academicYear.Id))
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
            return View(academicYear);
        }

        // POST: AcademicYearManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var academicYear = await _context.AcademicYears
                .Include(ay => ay.Users)
                .FirstOrDefaultAsync(ay => ay.Id == id);

            if (academicYear == null)
            {
                TempData["ErrorMessage"] = "Academic year not found.";
                return RedirectToAction(nameof(Index));
            }

            if (academicYear.Users.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete academic year that has associated users.";
                return RedirectToAction(nameof(Index));
            }

            _context.AcademicYears.Remove(academicYear);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Academic year deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool AcademicYearExists(int id)
        {
            return _context.AcademicYears.Any(e => e.Id == id);
        }
    }
}

