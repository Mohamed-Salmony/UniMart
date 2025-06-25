using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using UniMart_App.Data;
using UniMart_App.Models;

namespace UniMart_App.Controllers
{
    public class OrdersController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
            : base(userManager, signInManager)
        {
            _context = context;
        }

        // GET: /Orders/MyOrders
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Eagerly load all product images for all products in these orders
            var productIds = orders.SelectMany(o => o.OrderItems).Select(oi => oi.ProductId).Distinct().ToList();
            var productsWithImages = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Include(p => p.ProductImages)
                .ToListAsync();

            // Attach loaded images to products in order items
            foreach (var order in orders)
            {
                foreach (var item in order.OrderItems)
                {
                    item.Product.ProductImages = productsWithImages
                        .FirstOrDefault(p => p.Id == item.ProductId)?.ProductImages ?? new List<ProductImage>();
                }
            }

            return View(orders);
        }
    }
}
