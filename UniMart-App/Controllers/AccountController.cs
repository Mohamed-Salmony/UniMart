using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Data;
using UniMart_App.Models;
using UniMart_App.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace UniMart_App.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }


        // GET: Account/SignUp
        [HttpGet]
        public IActionResult SignUp()
        {
            var model = new SignUpViewModel();
            return View(model);
        }


        // POST: Account/SignUp
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
                FacultyId = null, // Set FacultyId to null
                AcademicYearId = null, // Set AcademicYearId to null
                ProfileImageUrl = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(model.FullName) + "&background=random", // Use dynamic avatar generation service
                EmailConfirmed = true // Consider implementing email confirmation later
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Assign role based on selection
                string role = model.Role ?? "User";
                await _userManager.AddToRoleAsync(user, role);
                
                await _signInManager.SignInAsync(user, isPersistent: false);
                
                // Redirect based on role
                return role switch
                {
                    "Merchant" => RedirectToAction("Dashboard", "Merchant"),
                    "Customer" => RedirectToAction("Index", "Home"),
                    _ => RedirectToAction("Index", "Home")
                };
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // Check if user is a suspended merchant
            if (await _userManager.IsInRoleAsync(user, "Merchant") && user.IsSuspended)
            {
                ModelState.AddModelError(string.Empty, "Your merchant account has been suspended. Please contact support for more information.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                user.LastSeen = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                // Save login history
                var device = Request.Headers["User-Agent"].ToString();
                var location = "Unknown"; // You can enhance this with IP-based geolocation if needed
                var loginHistory = new LoginHistory
                {
                    UserId = user.Id,
                    LoginTime = DateTime.UtcNow,
                    Device = device,
                    Location = location
                };
                _context.LoginHistories.Add(loginHistory);
                await _context.SaveChangesAsync();

                if (string.IsNullOrEmpty(returnUrl))
                {
                    // Redirect based on user role
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else if (await _userManager.IsInRoleAsync(user, "Merchant"))
                    {
                        return RedirectToAction("Dashboard", "Merchant");
                    }
                    return RedirectToAction("Index", "Home");
                }
                return LocalRedirect(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }


        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email not registered");
                return View(model);
            }

            // Send password reset link
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account",
                new { userId = user.Id, code }, protocol: HttpContext.Request.Scheme);

            // TODO: Send email here

            TempData["Success"] = "A reset link has been sent to your email.";
            return RedirectToAction("Login");
        }

        // GET: Account/Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Load related entities
            user = await _context.Users
                .Include(u => u.Faculty)
                .Include(u => u.AcademicYear)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            // Get recent orders
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var recentOrders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(3)
                .ToListAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            ViewBag.RecentOrders = recentOrders;

            // Get favorite products
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var favoriteProducts = await _context.Favorites
                .Where(f => f.UserId == user.Id)
                .Include(f => f.Product)
                    .ThenInclude(p => p.ProductImages)
                .OrderByDescending(f => f.Id)
                .Select(f => f.Product)
                .Take(2)
                .ToListAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            ViewBag.FavoriteProducts = favoriteProducts;

            // Get faculties and academic years for dropdowns
            ViewBag.Faculties = await _context.Faculties.ToListAsync();
            ViewBag.AcademicYears = await _context.AcademicYears.ToListAsync();

            // Get login history
            var loginHistory = await _context.LoginHistories
                .Where(lh => lh.UserId == user.Id)
                .OrderByDescending(lh => lh.LoginTime)
                .Take(10)
                .ToListAsync();
            ViewBag.LoginHistory = loginHistory;

            return View("Profile/Profile", user);
        }

        // POST: Account/UpdateAddress
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAddress(string address, string city, string postalCode, double? latitude, double? longitude)
        {
            var user = await _userManager.GetUserAsync(User);
#pragma warning disable CS8602 // Dereference of a possibly null reference.

            user.Address = address;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            user.City = city;
            user.PostalCode = postalCode;
            user.Latitude = latitude;
            user.Longitude = longitude;
            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = "Address updated successfully.";
            
            return RedirectToAction("Profile");
        }

        // POST: Account/UpdateAcademic
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAcademic(int facultyId, int academicYearId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.FacultyId = facultyId;
            user.AcademicYearId = academicYearId;

            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = "Academic information updated successfully.";
            
            return RedirectToAction(nameof(Profile));
        }

        // POST: Account/LogoutAllSessions
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutAllSessions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Change security stamp to invalidate all existing sessions
            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.SignOutAsync();
            
            TempData["SuccessMessage"] = "You have been logged out from all sessions.";
            return RedirectToAction("Login");
        }

        // GET: Account/Notifications
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Load user's notifications
            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // GET: Account/Settings
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

#pragma warning disable CS8601 // Possible null reference assignment.

            var settingsViewModel = new SettingsViewModel
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = user.FullName,
                ProfileImageUrl = user.ProfileImageUrl
            };
#pragma warning restore CS8601 // Possible null reference assignment.


            return View(settingsViewModel);
        }

        // POST: Account/UpdateSettings
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(SettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                return Json(new { success = false, errors });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, errors = new[] { "User not found." } });
            }

            // Update user information
            user.PhoneNumber = model.PhoneNumber;
            if (user.FullName != model.FullName)
            {
                user.FullName = model.FullName;
                var nameParts = model.FullName.Trim().Split(' ');
                user.FirstName = nameParts[0];
                user.LastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";
                if (user.ProfileImageUrl != null && user.ProfileImageUrl.Contains("ui-avatars.com"))
                {
                    user.ProfileImageUrl = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(model.FullName) + "&background=random";
                }
            }

            // Handle profile image upload if provided
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img", "profile");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(stream);
                }
                user.ProfileImageUrl = $"/img/profile/{fileName}";
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true, profileImageUrl = user.ProfileImageUrl });
            }

            var updateErrors = result.Errors.Select(e => e.Description).ToArray();
            return Json(new { success = false, errors = updateErrors });
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8601 // Possible null reference assignment.

                var settingsViewModel = new SettingsViewModel
                {
                    Email = User.Identity.Name
                };
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8602 // Dereference of a possibly null reference.


                return View("Settings", settingsViewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToAction(nameof(Settings));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

#pragma warning disable CS8601 // Possible null reference assignment.

            var settingsVm = new SettingsViewModel
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = user.FullName,
                ProfileImageUrl = user.ProfileImageUrl
            };
#pragma warning restore CS8601 // Possible null reference assignment.


            return View("Settings", settingsVm);
        }

        // POST: Account/UploadProfileImage
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, error = "No file uploaded." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, error = "User not found." });

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img", "profile");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            user.ProfileImageUrl = $"/img/profile/{fileName}";
            await _userManager.UpdateAsync(user);

            return Json(new { success = true, profileImageUrl = user.ProfileImageUrl });
        }
    }
}
