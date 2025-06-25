using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Data;
using UniMart_App.Models;

namespace UniMart_App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            // Check if user is authenticated
#pragma warning disable CS8602 // Dereference of a possibly null reference.

            if (User.Identity.IsAuthenticated)
            {
                // Load faculties for the dropdown
                ViewBag.Faculties = await _context.Faculties.ToListAsync();
                // Load trending products (top 4 by number of reviews, approved only)
                var trendingProducts = await _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.Ratings)
                    .Where(p => p.IsApproved)
                    .OrderByDescending(p => p.Ratings.Count)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(4)
                    .ToListAsync();
                return View("LoggedInIndex", trendingProducts);
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            // Not authenticated, show the before login page
            // Load trending products (top 4 by rating, approved only)
            var trendingProductsUnauth = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.IsApproved)
                .OrderByDescending(p => p.Rating)
                .ThenByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();
            return View(trendingProductsUnauth);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // GET: /Home/SeedData
        public async Task<IActionResult> SeedData()
        {
            try
            {
                // Create roles
                string[] roles = { "Admin", "User" };
                foreach (var role in roles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                // Seed Faculties if empty
                if (!await _context.Faculties.AnyAsync())
                {
                    var faculties = new[]
                    {
                        new Faculty { Name = "Engineering", Description = "Natural and Applied Sciences" },
                        new Faculty { Name = "Pharmacy", Description = "Pharmacy and Healthcare Sciences" },
                        new Faculty { Name = "Sciences", Description = "Natural and Applied Sciences" },
                        new Faculty { Name = "Medicine", Description = "Human Medicine and Health Sciences" },
                        new Faculty { Name = "Business Administration", Description = "Management and Economics" },
                        new Faculty { Name = "Education", Description = "Humanities and Social Sciences" }
                    };

                    await _context.Faculties.AddRangeAsync(faculties);
                    await _context.SaveChangesAsync();
                }

                // Seed Academic Years if empty
                if (!await _context.AcademicYears.AnyAsync())
                {
                    var academicYears = new[]
                    {
                        new AcademicYear { Year = "First Year" },
                        new AcademicYear { Year = "Second Year" },
                        new AcademicYear { Year = "Third Year" },
                        new AcademicYear { Year = "Fourth Year" },
                        new AcademicYear { Year = "Fifth Year" }
                    };

                    await _context.AcademicYears.AddRangeAsync(academicYears);
                    await _context.SaveChangesAsync();
                }

                // Create admin user if it doesn't exist
                var adminEmail = "admin@unimart.com";
                var adminUser = await _userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    var firstFaculty = await _context.Faculties.OrderBy(f => f.Id).FirstOrDefaultAsync();
                    var firstYear = await _context.AcademicYears.OrderBy(y => y.Id).FirstOrDefaultAsync();

                    if (firstFaculty != null && firstYear != null)
                    {
                        var admin = new ApplicationUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            EmailConfirmed = true,
                            FullName = "Admin User",
                            FacultyId = firstFaculty.Id,
                            AcademicYearId = firstYear.Id
                        };

                        var result = await _userManager.CreateAsync(admin, "Admin123!");
                        if (result.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(admin, "Admin");
                        }
                    }
                }

                return Content("Database seeded successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding database");
                return Content($"Error seeding database: {ex.Message}");
            }
        }

        // GET: /Home/Faculty/{id}
        public IActionResult Faculty(int id)
        {
            var faculty = _context.Faculties
                .Include(f => f.Products)
                    .ThenInclude(p => p.ProductImages)
                .Include(f => f.Products)
                    .ThenInclude(p => p.Ratings)
                .FirstOrDefault(f => f.Id == id);

            if (faculty == null)
            {
                return NotFound();
            }
            return View(faculty);
        }

        // GET: /Home/Faculties
        public async Task<IActionResult> Faculties()
        {
            var faculties = await _context.Faculties.ToListAsync();
            return View(faculties);
        }

        // GET: /Home/Products
        public IActionResult Products()
        {
            // In a real implementation, you would load products from the database
            return View();
        }

        // GET: /Home/Search
        public IActionResult Search(string query)
        {
            ViewBag.SearchQuery = query;
            // In a real implementation, you would search products based on the query
            return View();
        }

        // GET: /Home/About
        public IActionResult About()
        {
            // Redirect to Contact page's Team section
            return Redirect(Url.Action("Index", "Contact") + "#teamSection");
        }

        // GET: /Home/Contact
        public IActionResult Contact()
        {
            return View();
        }

        // GET: /Home/FAQ
        public IActionResult FAQ()
        {
            return View(Url.Action("Index", "Contact") + "#FAQs");
        }

        // GET: /Home/Shipping
        public IActionResult Shipping()
        {
            return View(Url.Action("Index", "Contact") + "#collapseFour");
        }

       // GET: /Home/ImageTest
        public IActionResult ImageTest()
        {
            return View();
        }
    }
}
