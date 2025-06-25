using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using UniMart_App.Models;
using UniMart_App.Data;
using Microsoft.EntityFrameworkCore;
using UniMart_App.ViewModels;

namespace UniMart_App.Controllers
{
    public class MerchantRegistrationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public MerchantRegistrationController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: MerchantRegistration/SignUp
        [HttpGet]
        public IActionResult SignUp()
        {
            var model = new SignUpViewModel();
            return View(model);
        }

        // POST: MerchantRegistration/SignUp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            // Get default faculty and academic year (first ones in the database)
            var defaultFaculty = await _context.Faculties.OrderBy(f => f.Id).FirstOrDefaultAsync();
            var defaultAcademicYear = await _context.AcademicYears.OrderBy(y => y.Id).FirstOrDefaultAsync();

            if (defaultFaculty == null || defaultAcademicYear == null)
            {
                ModelState.AddModelError(string.Empty, "System configuration error. Please contact support.");
                return View(model);
            }

            // Split full name into first name and last name
            var nameParts = model.FullName.Trim().Split(' ');
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

            var user = new ApplicationUser
            {
                UserName = model.Email,
                FullName = model.FullName,
                FirstName = firstName,
                LastName = lastName,
                Email = model.Email,
                FacultyId = defaultFaculty.Id,
                AcademicYearId = defaultAcademicYear.Id,
                MerchantStatus = MerchantStatus.Pending, // Set initial status as Pending
                ProfileImageUrl = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(model.FullName) + "&background=random",
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Assign "Merchant" role to merchant registrations
                await _userManager.AddToRoleAsync(user, "Merchant");
                
                await _signInManager.SignInAsync(user, isPersistent: false);
                
                // Redirect to merchant dashboard
                return RedirectToAction("Dashboard", "Merchant");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}

