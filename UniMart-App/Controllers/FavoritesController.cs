using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Data;
using UniMart_App.Models;

namespace UniMart_App.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoritesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "Products", new { id = productId }) });
            }

            var exists = await _context.Favorites.AnyAsync(f => f.UserId == userId && f.ProductId == productId);
            if (!exists)
            {
                var favorite = new Favorite { UserId = userId, ProductId = productId };
                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Added to favorites.";
            }
            else
            {
                TempData["Info"] = "Already in favorites.";
            }
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Removed from favorites.";
            }
            else
            {
                TempData["Info"] = "Product was not in your favorites.";
            }
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var favorites = await _context.Favorites
                .Include(f => f.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Faculty)
                .Where(f => f.UserId == userId)
                .ToListAsync();
            return View(favorites);
        }
    }
}
