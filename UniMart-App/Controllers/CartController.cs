using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using UniMart_App.Data;
using UniMart_App.Helpers;
using UniMart_App.Models;
using UniMart_App.Services;

namespace UniMart_App.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                    .ThenInclude(p => p.Faculty)
                .Include(c => c.Product)
                    .ThenInclude(p => p.User)
                .Include(c => c.Product)
                    .ThenInclude(p => p.ProductImages) // Ensure images are included
                .Where(c => c.UserId == userId)
                .ToListAsync();
            await SetOrderSummaryViewBag(cartItems);
            return View(cartItems);
        }

        [HttpPost]
        [AllowAnonymous] // Add this for debugging
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                // For standard POST, redirect to login
                if (!Request.Headers["X-Requested-With"].Equals("XMLHttpRequest"))
                    return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Products") });
                return Json(new { success = false, message = "You must be logged in to add products to your cart." });
            }
            var product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null || product.StockQuantity < quantity)
            {
                if (!Request.Headers["X-Requested-With"].Equals("XMLHttpRequest"))
                {
                    TempData["Error"] = "Product not available or insufficient stock.";
                    return RedirectToAction("Index", "Products");
                }
                return Json(new { success = false, message = "Product not available or insufficient stock" });
            }
            var existingCartItem = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
            if (existingCartItem != null)
            {
                existingCartItem.Quantity += quantity;
                if (existingCartItem.Quantity > product.StockQuantity)
                {
                    existingCartItem.Quantity = product.StockQuantity;
                }
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    SelectedOptions = "" // Ensure not null
                };
                _context.CartItems.Add(cartItem);
            }
            await _context.SaveChangesAsync();
            await _notificationService.CreateNotificationAsync(
                userId,
                "Product Added to Cart",
                $"'{product.Name}' has been added to your cart.",
                "System",
                "/Cart/Index");
            // For standard POST, set TempData and redirect
            if (!Request.Headers["X-Requested-With"].Equals("XMLHttpRequest"))
            {
                TempData["Success"] = $"'{product.Name}' has been added to your cart.";
                var returnUrl = Request.Form["returnUrl"].ToString();
                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Products");
            }
            return Json(new { success = true, message = "Product added to cart" });
        }

        // [HttpPost]
        // public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        // {
        //     var userId = _userManager.GetUserId(User);
        //     var cartItem = await _context.CartItems
        //         .Include(c => c.Product)
        //         .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

        //     if (cartItem == null)
        //     {
        //         return Json(new { success = false, message = "Cart item not found" });
        //     }

        //     if (quantity <= 0)
        //     {
        //         _context.CartItems.Remove(cartItem);
        //     }
        //     else if (quantity <= cartItem.Product.StockQuantity)
        //     {
        //         cartItem.Quantity = quantity;
        //     }
        //     else
        //     {
        //         return Json(new { success = false, message = "Insufficient stock" });
        //     }

        //     await _context.SaveChangesAsync();
        //     return Json(new { success = true });
        // }

        // [HttpPost]
        // public async Task<IActionResult> RemoveFromCart(int cartItemId)
        // {
        //     var userId = _userManager.GetUserId(User);
        //     var cartItem = await _context.CartItems
        //         .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

        //     if (cartItem != null)
        //     {
        //         _context.CartItems.Remove(cartItem);
        //         await _context.SaveChangesAsync();
        //     }

        //     return RedirectToAction(nameof(Index));
        // }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty";
                return RedirectToAction(nameof(Index));
            }

            // Check stock availability
            foreach (var item in cartItems)
            {
                if (item.Product.StockQuantity < item.Quantity)
                {
                    TempData["ErrorMessage"] = $"Insufficient stock for {item.Product.Name}";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Create order
            var shippingAddress = TempData["ShippingAddress"] as string;
            var phoneNumber = TempData["PhoneNumber"] as string;
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                ShippingAddress = shippingAddress,
                PhoneNumber = phoneNumber,
                TrackingNumber = $"TRK-{Guid.NewGuid().ToString("N").Substring(0, 10)}",
                ShippingStatus = "Pending" // Set default shipping status
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create order items and update stock
            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Product.Price
                };

                _context.OrderItems.Add(orderItem);

                // Update stock
                cartItem.Product.StockQuantity -= cartItem.Quantity;

                // Low stock notification
                if (cartItem.Product.StockQuantity < 5)
                {
                    await _notificationService.CreateNotificationAsync(
                        cartItem.Product.UserId,
                        "Low Stock Alert",
                        $"Your product '{cartItem.Product.Name}' is low in stock.",
                        "Alert",
                        "/Merchant/Inventory");
                }
            }

            // Clear cart
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // Notify merchants for new order
            var orderItems = await _context.OrderItems.Include(oi => oi.Product).Where(oi => oi.OrderId == order.Id).ToListAsync();
            var notifiedMerchants = new HashSet<string>();
            foreach (var item in orderItems)
            {
                if (!notifiedMerchants.Contains(item.Product.UserId))
                {
                    await _notificationService.CreateNotificationAsync(
                        item.Product.UserId,
                        "New Order Received",
                        $"You have received a new order for {item.Product.Name}.",
                        "Order",
                        "/Merchant/Orders");
                    notifiedMerchants.Add(item.Product.UserId);
                }
            }

            TempData["SuccessMessage"] = "Order placed successfully!";
            return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
        }

        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> GetCartCount()
        {
            var userId = _userManager.GetUserId(User);
            var count = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> GetCartItemsPartial()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                    .ThenInclude(p => p.ProductImages)
                .Where(c => c.UserId == userId)
                .ToListAsync();
            return PartialView("_CartItemsPartial", cartItems);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderSummaryPartial()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();
            await SetOrderSummaryViewBag(cartItems);
            return PartialView("_OrderSummaryPartial");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] CartQuantityUpdateModel model)
        {
            var userId = _userManager.GetUserId(User);
            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == model.CartItemId && c.UserId == userId);

            if (cartItem == null)
                return Json(new { success = false, message = "Cart item not found" });

            if (model.Quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
            }
            else if (model.Quantity <= cartItem.Product.StockQuantity)
            {
                cartItem.Quantity = model.Quantity;
            }
            else
            {
                return Json(new { success = false, message = "Insufficient stock" });
            }

            await _context.SaveChangesAsync();
            var cartHtml = await RenderViewAsync("_CartItemsPartial");
            var summaryHtml = await RenderViewAsync("_OrderSummaryPartial");
            return Json(new { success = true, cartHtml, summaryHtml });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart([FromBody] CartRemoveModel model)
        {
            var userId = _userManager.GetUserId(User);
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == model.CartItemId && c.UserId == userId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            var cartHtml = await RenderViewAsync("_CartItemsPartial");
            var summaryHtml = await RenderViewAsync("_OrderSummaryPartial");
            return Json(new { success = true, cartHtml, summaryHtml });
        }

        [HttpGet]
        public IActionResult Payment()
        {
            return View(new PaymentViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Payment(PaymentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Here you would integrate with a payment gateway. For now, assume payment is successful.
            // After payment, create the order (reuse your Checkout logic, or refactor it into a method)
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty";
                return RedirectToAction(nameof(Index));
            }

            // Check stock availability
            foreach (var item in cartItems)
            {
                if (item.Product.StockQuantity < item.Quantity)
                {
                    TempData["ErrorMessage"] = $"Insufficient stock for {item.Product.Name}";
                    return RedirectToAction(nameof(Index));
                }
            }

            var shippingAddress = TempData["ShippingAddress"] as string;
            var phoneNumber = TempData["PhoneNumber"] as string;
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                ShippingAddress = shippingAddress,
                PhoneNumber = phoneNumber,
                TrackingNumber = $"TRK-{Guid.NewGuid().ToString("N").Substring(0, 10)}",
                ShippingStatus = "Pending" // Set default shipping status
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Product.Price
                };
                _context.OrderItems.Add(orderItem);
                cartItem.Product.StockQuantity -= cartItem.Quantity;
                if (cartItem.Product.StockQuantity < 5)
                {
                    await _notificationService.CreateNotificationAsync(
                        cartItem.Product.UserId,
                        "Low Stock Alert",
                        $"Your product '{cartItem.Product.Name}' is low in stock.",
                        "Alert",
                        "/Merchant/Inventory");
                }
            }
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // Notify merchants for new order
            var orderItems = await _context.OrderItems.Include(oi => oi.Product).Where(oi => oi.OrderId == order.Id).ToListAsync();
            var notifiedMerchants = new HashSet<string>();
            foreach (var item in orderItems)
            {
                if (!notifiedMerchants.Contains(item.Product.UserId))
                {
                    await _notificationService.CreateNotificationAsync(
                        item.Product.UserId,
                        "New Order Received",
                        $"You have received a new order for {item.Product.Name}.",
                        "Order",
                        "/Merchant/Orders");
                    notifiedMerchants.Add(item.Product.UserId);
                }
            }
            TempData["SuccessMessage"] = "Order placed successfully!";
            return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
        }

        [HttpPost]
        public async Task<IActionResult> Shipping(ShippingViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.Address = model.Address;
                user.PhoneNumber = model.PhoneNumber;
                await _userManager.UpdateAsync(user);
            }
            // Store shipping info in TempData for order creation
            TempData["ShippingAddress"] = model.Address;
            TempData["PhoneNumber"] = model.PhoneNumber;
            // Redirect to payment step
            return RedirectToAction("Payment");
        }

        [HttpGet]
        public async Task<IActionResult> Shipping()
        {
            var user = await _userManager.GetUserAsync(User);
            var model = new ShippingViewModel();
            if (user != null)
            {
                model.Address = user.Address ?? string.Empty;
                model.PhoneNumber = user.PhoneNumber ?? string.Empty;
            }
            return View(model);
        }

        // Helper to render partials as string
        private async Task<string> RenderViewAsync(string viewName)
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                    .ThenInclude(p => p.ProductImages)
                .Where(c => c.UserId == userId)
                .ToListAsync();
            if (viewName == "_CartItemsPartial")
                return await this.RenderViewToStringAsync("~/Views/Cart/_CartItemsPartial.cshtml", cartItems);
            decimal subtotal = cartItems.Sum(i => i.Product.Price * i.Quantity);
            decimal shipping = 0;
            // Get dynamic tax percentage from Settings
            var settings = await _context.Settings.FirstOrDefaultAsync();
            decimal taxPercent = settings?.UserFeePercentage ?? 8m; // fallback to 8 if not set
            decimal tax = subtotal * (taxPercent / 100m);
            decimal total = subtotal + shipping + tax;
            ViewBag.Subtotal = subtotal;
            ViewBag.Shipping = shipping;
            ViewBag.Tax = tax;
            ViewBag.Total = total;
            ViewBag.TaxPercent = taxPercent;
            return await this.RenderViewToStringAsync("~/Views/Cart/_OrderSummaryPartial.cshtml", null);
        }

        // Helper to set order summary ViewBag values
        private async Task SetOrderSummaryViewBag(List<CartItem> cartItems)
        {
            decimal subtotal = cartItems.Sum(i => i.Product.Price * i.Quantity);
            // Get dynamic shipping and tax percentage from Settings
            var settings = await _context.Settings.FirstOrDefaultAsync();
            decimal shipping = settings?.UserRewardPercentage ?? 0m; // Use UserRewardPercentage as shipping fee (or add a ShippingFee property if needed)
            decimal taxPercent = settings?.UserFeePercentage ?? 8m; // fallback to 8 if not set
            decimal tax = subtotal * (taxPercent / 100m);
            decimal total = subtotal + shipping + tax;
            ViewBag.Subtotal = subtotal;
            ViewBag.Shipping = shipping;
            ViewBag.Tax = tax;
            ViewBag.Total = total;
            ViewBag.TaxPercent = taxPercent;
        }
    }

    public class CartQuantityUpdateModel
    {
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }
    public class CartRemoveModel
    {
        public int CartItemId { get; set; }
    }

    // Add this ViewModel in your project (e.g., in ViewModels folder)
    public class ShippingViewModel
    {
        [Required]
        public string Address { get; set; } = string.Empty;
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}

