using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using UniMart_App.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Data;

namespace UniMart_App.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public DashboardController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get counts for dashboard statistics
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
            var activeUsers = await _userManager.Users.CountAsync();
            var dailyOrders = await _context.Orders
                .Where(o => o.OrderDate.Date == DateTime.Today)
                .CountAsync();

            // Get recent orders for the activity table
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.User.Faculty)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            // Pass data to the view
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.ActiveUsers = activeUsers;
            ViewBag.DailyOrders = dailyOrders;
            ViewBag.RecentOrders = recentOrders;

            return View();
        }

        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Faculty)
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> Faculties()
        {
            var faculties = await _context.Faculties
                .Select(f => new
                {
                    Faculty = f,
                    ProductCount = _context.Products.Count(p => p.FacultyId == f.Id)
                })
                .ToListAsync();

            return View(faculties);
        }

        [HttpGet]
        public async Task<IActionResult> ManageAdmins()
        {
            var adminRole = await _roleManager.FindByNameAsync("Admin");
            if (adminRole == null)
            {
                return NotFound("Admin role not found");
            }

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var regularUsers = await _userManager.Users
                .Where(u => !adminUsers.Select(a => a.Id).Contains(u.Id))
                .ToListAsync();

            ViewBag.AdminUsers = adminUsers;
            ViewBag.RegularUsers = regularUsers;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Failed to add user to admin role");
                return RedirectToAction(nameof(ManageAdmins));
            }

            return RedirectToAction(nameof(ManageAdmins));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAdmin(string userId)
        {
            // Get the current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound("Current user not found");
            }

            // Don't allow removing yourself as admin
            if (currentUser.Id == userId)
            {
                ModelState.AddModelError("", "You cannot remove yourself as an admin");
                return RedirectToAction(nameof(ManageAdmins));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Failed to remove user from admin role");
                return RedirectToAction(nameof(ManageAdmins));
            }

            return RedirectToAction(nameof(ManageAdmins));
        }
    }
}
