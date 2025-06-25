using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniMart_App.Data;
using UniMart_App.Models;
using UniMart_App.ViewModels;

namespace UniMart_App.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatalogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Catalog
        public async Task<IActionResult> Index(string faculty = null, string search = null, string sort = null)
        {
            // Start with all products
            var productsQuery = _context.Products
                .Include(p => p.Faculty)
                .Include(p => p.User)
                .AsQueryable();

            // Apply faculty filter if provided
            if (!string.IsNullOrEmpty(faculty))
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                productsQuery = productsQuery.Where(p => p.Faculty.Name == faculty);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(search))
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(search) ||
                    p.Description.Contains(search) ||
                    p.Faculty.Name.Contains(search));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }

            // Apply sorting
            switch (sort)
            {
                case "price_asc":
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                case "newest":
                    productsQuery = productsQuery.OrderByDescending(p => p.CreatedAt);
                    break;
                case "oldest":
                    productsQuery = productsQuery.OrderBy(p => p.CreatedAt);
                    break;
                case "name_asc":
                    productsQuery = productsQuery.OrderBy(p => p.Name);
                    break;
                case "name_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Name);
                    break;
                default:
                    productsQuery = productsQuery.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            // Get all faculties for the filter dropdown
            var faculties = await _context.Faculties.ToListAsync();

            // Execute the query and create the view model
            var products = await productsQuery.ToListAsync();

            var viewModel = new CatalogViewModel
            {
                Products = products,
                Faculties = faculties,
                CurrentFaculty = faculty,
                SearchQuery = search,
                SortOption = sort ?? "newest"
            };

            return View(viewModel);
        }

        // GET: Catalog/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Faculty)
                .Include(p => p.User)
                .Include(p => p.Ratings)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Get related products from the same faculty
            var relatedProducts = await _context.Products
                .Where(p => p.FacultyId == product.FacultyId && p.Id != product.Id)
                .Take(4)
                .ToListAsync();

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts,
                Rating = new RatingViewModel { ProductId = product.Id }
            };

            return View(viewModel);
        }

        // POST: Catalog/AddRating
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRating(RatingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Details), new { id = model.ProductId });
            }

            // Check if user is authenticated
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Check if user has already rated this product
            var existingRating = await _context.ProductRatings
                .FirstOrDefaultAsync(r => r.ProductId == model.ProductId && r.UserId == userId);

            if (existingRating != null)
            {
                // Update existing rating
                existingRating.Rating = model.Rating;
                existingRating.Comment = model.Comment;
            }
            else
            {
                // Create new rating
                var rating = new ProductRating
                {
                    ProductId = model.ProductId,
                    UserId = userId,
                    Rating = model.Rating,
                    Comment = model.Comment
                };

                _context.ProductRatings.Add(rating);
            }

            await _context.SaveChangesAsync();

            // Update product average rating
            var product = await _context.Products.FindAsync(model.ProductId);
            if (product != null)
            {
                var ratings = await _context.ProductRatings
                    .Where(r => r.ProductId == model.ProductId)
                    .ToListAsync();

                // Calculate average rating but don't assign to read-only property
                // Instead, update the database directly or use a writable property
                if (ratings.Any())
                {
                    // Assuming there's a writable AverageRating property or similar field
                    // that can be updated in the database
                    product.UpdateAverageRating((decimal)ratings.Average(r => r.Rating));
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }

        // POST: Catalog/AddToFavorites/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorites(int id)
        {
            // Check if user is authenticated
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Check if product exists
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Check if already in favorites
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.ProductId == id && f.UserId == userId);

            if (existingFavorite == null)
            {
                // Add to favorites
                var favorite = new Favorite
                {
                    ProductId = id,
                    UserId = userId
                };

                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Catalog/RemoveFromFavorites/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromFavorites(int id)
        {
            // Check if user is authenticated
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Find the favorite entry
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.ProductId == id && f.UserId == userId);

            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}