using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Data;
using UniMart_App.Models;
using UniMart_App.Services;

namespace UniMart_App.Controllers
{
    [Authorize]
    public class ProductRatingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public ProductRatingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<IActionResult> AddRating(int productId, int rating, string comment)
        {
            var userId = _userManager.GetUserId(User);
            
            // Check if product exists and is approved
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsApproved);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            // Check if user has already rated this product
            var existingRating = await _context.ProductRatings
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (existingRating != null)
            {
                // Update existing rating
                existingRating.RatingValue = rating;
                existingRating.Comment = comment ?? "";
                existingRating.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new rating
                var productRating = new ProductRating
                {
                    ProductId = productId,
                    UserId = userId,
                    RatingValue = rating,
                    Comment = comment ?? "",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProductRatings.Add(productRating);
            }

            await _context.SaveChangesAsync();
            // Notify merchant
            if (product != null)
            {
                await _notificationService.CreateNotificationAsync(
                    product.UserId,
                    "New Product Review",
                    $"Your product '{product.Name}' received a new review.",
                    "Message",
                    $"/Merchant/Products");
            }

            // Update product's average rating
            await UpdateProductAverageRating(productId);

            var user = await _userManager.GetUserAsync(User);
            var userName = user?.UserName ?? "You";
            var createdAt = DateTime.Now.ToString("MMM dd, yyyy");

            // Return JSON for AJAX
            return Json(new
            {
                success = true,
                ratingValue = rating,
                userName = userName,
                createdAt = createdAt,
                comment = comment
            });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRating(int ratingId)
        {
            var userId = _userManager.GetUserId(User);
            var rating = await _context.ProductRatings
                .FirstOrDefaultAsync(r => r.Id == ratingId && r.UserId == userId);

            if (rating == null)
            {
                return Json(new { success = false, message = "Rating not found" });
            }

            var productId = rating.ProductId;
            _context.ProductRatings.Remove(rating);
            await _context.SaveChangesAsync();

            // Update product's average rating
            await UpdateProductAverageRating(productId);

            return Json(new { success = true, message = "Rating deleted successfully" });
        }

        public async Task<IActionResult> GetProductRatings(int productId)
        {
            var ratings = await _context.ProductRatings
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    userName = r.User.FullName,
                    rating = r.RatingValue,
                    comment = r.Comment,
                    createdAt = r.CreatedAt.ToString("MMM dd, yyyy"),
                    isCurrentUser = r.UserId == _userManager.GetUserId(User)
                })
                .ToListAsync();

            return Json(ratings);
        }

        private async Task UpdateProductAverageRating(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                var ratings = await _context.ProductRatings
                    .Where(r => r.ProductId == productId)
                    .ToListAsync();

                if (ratings.Any())
                {
                    product.Rating = (decimal)ratings.Average(r => r.RatingValue);
                }
                else
                {
                    product.Rating = null;
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}

