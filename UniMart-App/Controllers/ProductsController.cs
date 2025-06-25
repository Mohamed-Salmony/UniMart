using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Data;
using UniMart_App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace UniMart_App.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? facultyId, decimal? minPrice, decimal? maxPrice, int? minRating, string sortBy = "popularity")
        {
            try
            {
                _logger.LogInformation("Fetching products for index page with filters");
                
                var query = _context.Products
                    .Include(p => p.Faculty)
                    .Include(p => p.User)
                    .Include(p => p.Ratings)
                    .Include(p => p.ProductImages)
                    .Where(p => p.StockQuantity > 0 && p.IsApproved);

                int selectedFacultyId = facultyId ?? 0;
                if (selectedFacultyId > 0)
                {
                    query = query.Where(p => p.FacultyId == selectedFacultyId);
                }
                if (minPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= minPrice.Value);
                }
                if (maxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= maxPrice.Value);
                }

                // Move to memory before filtering by AverageRating
                var productsList = await query.ToListAsync();

                if (minRating.HasValue)
                {
                    productsList = productsList.Where(p => p.AverageRating >= (decimal)minRating.Value).ToList();
                }

                // Sorting (in memory if needed)
                switch (sortBy?.ToLower())
                {
                    case "price_low_high":
                        productsList = productsList.OrderBy(p => p.Price).ToList();
                        break;
                    case "price_high_low":
                        productsList = productsList.OrderByDescending(p => p.Price).ToList();
                        break;
                    case "newest":
                        productsList = productsList.OrderByDescending(p => p.CreatedAt).ToList();
                        break;
                    default:
                        productsList = productsList.OrderByDescending(p => p.Ratings.Count).ToList();
                        break;
                }

                // Get all faculties for the filter dropdown
                var faculties = await _context.Faculties.ToListAsync();

                ViewBag.Faculties = faculties;
                ViewBag.SelectedFacultyId = selectedFacultyId;
                ViewBag.MinPrice = minPrice;
                ViewBag.MaxPrice = maxPrice;
                ViewBag.MinRating = minRating;
                ViewBag.SortBy = sortBy;

                _logger.LogInformation($"Found {productsList.Count} products after applying filters");
                return View(productsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products for index page");
                throw;
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                _logger.LogInformation($"Fetching details for product ID: {id}");
                
                var product = await _context.Products
                    .Include(p => p.Faculty)
                    .Include(p => p.User)
                    .Include(p => p.Ratings)
                        .ThenInclude(r => r.User)
                    .Include(p => p.ProductImages)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    _logger.LogWarning($"Product with ID {id} not found");
                    return NotFound();
                }

                // Check if the current user is either an admin or the product owner
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");
                var isMerchant = User.IsInRole("Merchant");
                var isProductOwner = currentUserId == product.UserId;

                // Only show the product if:
                // 1. It's approved OR
                // 2. The current user is an admin OR
                // 3. The current user is a merchant
                if (!product.IsApproved && !(isAdmin || isMerchant))
                {
                    _logger.LogWarning($"Unauthorized access attempt to unapproved product {id} by user {currentUserId}");
                    return NotFound();
                }

                // Get related products (same faculty, excluding current product)
                var relatedProducts = await _context.Products
                    .Include(p => p.Faculty)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Ratings) // Ensure ratings are included for correct average
                    .AsSplitQuery()
                    .Where(p => p.FacultyId == product.FacultyId && 
                               p.Id != product.Id && 
                               p.StockQuantity > 0 && 
                               p.IsApproved)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(3)
                    .ToListAsync();

                ViewBag.RelatedProducts = relatedProducts;
                ViewBag.IsProductOwner = isProductOwner;
                ViewBag.IsAdmin = isAdmin;

                _logger.LogInformation($"Found product: {product.Name}");
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching details for product ID: {id}");
                throw;
            }
        }

        public async Task<IActionResult> Search(string query)
        {
            ViewBag.SearchQuery = query;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

            var products = await _context.Products
                .Include(p => p.Faculty)
                .Where(p => p.StockQuantity > 0 && p.IsApproved && // Only show approved products
                    (string.IsNullOrEmpty(query) || 
                     p.Name.Contains(query) || 
                     p.Description.Contains(query) ||
                     p.Faculty.Name.Contains(query)))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.


            return View(products);
        }

        public async Task<IActionResult> Category(int facultyId)
        {
            var faculty = await _context.Faculties.FindAsync(facultyId);
            if (faculty == null)
            {
                return NotFound();
            }

            var products = await _context.Products
                .Include(p => p.Faculty)
                .Include(p => p.User)
                .Where(p => p.FacultyId == facultyId && p.StockQuantity > 0 && p.IsApproved) // Only show approved products
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.CategoryName = faculty.Name;
            return View(products);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToFavorites(int productId)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.

            var userId = User.Identity.Name;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userId);
            
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == user.Id && f.ProductId == productId);

            if (existingFavorite == null)
            {
                var favorite = new Favorite
                {
                    UserId = user.Id,
                    ProductId = productId
                };

                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Added to favorites" });
            }

            return Json(new { success = false, message = "Already in favorites" });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RateProduct(int productId, int rating, string comment)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var userId = User.Identity.Name;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userId);
            
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var existingRating = await _context.ProductRatings
                .FirstOrDefaultAsync(r => r.UserId == user.Id && r.ProductId == productId);

            if (existingRating != null)
            {
                existingRating.RatingValue = rating;
                existingRating.Comment = comment;
                existingRating.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                var productRating = new ProductRating
                {
                    UserId = user.Id,
                    ProductId = productId,
                    RatingValue = rating,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProductRatings.Add(productRating);
            }

            await _context.SaveChangesAsync();

            // Update product average rating
            var product = await _context.Products
                .Include(p => p.Ratings)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product != null)
            {
                product.UpdateAverageRating(product.AverageRating);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Rating submitted successfully" });
        }
    }
}
