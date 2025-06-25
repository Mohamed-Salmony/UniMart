using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Data;
using UniMart_App.Models;
using UniMart_App.Services;
using UniMart_App.ViewModels;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using ClosedXML.Excel;

namespace UniMart_App.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly IAntiforgery _antiforgery;
        private readonly ExcelExportService _excelExportService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            NotificationService notificationService,
            IAntiforgery antiforgery)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _notificationService = notificationService;
            _antiforgery = antiforgery;
            _excelExportService = new ExcelExportService();
        }

        public async Task<IActionResult> Index()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.OrderItems.SumAsync(oi => oi.Quantity * oi.Price);

            var adminStats = new
            {
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                AdminCount = await _userManager.GetUsersInRoleAsync("Admin").ContinueWith(t => t.Result.Count),
                UserCount = await _userManager.GetUsersInRoleAsync("User").ContinueWith(t => t.Result.Count),
                MerchantCount = await _userManager.GetUsersInRoleAsync("Merchant").ContinueWith(t => t.Result.Count)
            };

            // Get recent system activities
            var activities = new List<SystemActivity>();
            
            // Get recent merchant registrations
            var recentMerchants = await _userManager.GetUsersInRoleAsync("Merchant");
            var latestMerchant = recentMerchants
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();

            if (latestMerchant != null && latestMerchant.CreatedAt >= DateTime.UtcNow.AddDays(-1))
            {
                activities.Add(new SystemActivity
                {
                    Title = "New merchant registered",
                    Description = $"{latestMerchant.FullName} joined the marketplace",
                    Timestamp = latestMerchant.CreatedAt,
                    Icon = "bi-shop",
                    IconColor = "success",
                    ActionUrl = Url.Action("Merchants", "Admin") ?? "#"
                });
            }

            // Get recent product approvals
            var recentApprovedProduct = await _context.Products
                .Where(p => p.IsApproved && p.ApprovedAt != null)
                .OrderByDescending(p => p.ApprovedAt)
                .FirstOrDefaultAsync();

            if (recentApprovedProduct != null && recentApprovedProduct.ApprovedAt >= DateTime.UtcNow.AddDays(-1))
            {
#pragma warning disable CS8601 // Possible null reference assignment.

                activities.Add(new SystemActivity
                {
                    Title = "Product approved",
                    Description = $"{recentApprovedProduct.Name} approved for sale",
                    Timestamp = recentApprovedProduct.ApprovedAt.Value,
                    Icon = "bi-check-circle",
                    IconColor = "success",
                    ActionUrl = Url.Action("Products", "Admin")
                });
#pragma warning restore CS8601 // Possible null reference assignment.

            }

            // Get recent disputed orders (assuming status field is used for disputes)
            var recentDispute = await _context.Orders
                .Where(o => o.Status == "Disputed") // Adjust this based on your Order status enum/field
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefaultAsync();

            if (recentDispute != null && recentDispute.OrderDate >= DateTime.UtcNow.AddDays(-1))
            {
                activities.Add(new SystemActivity
                {
                    Title = "Order dispute reported",
                    Description = $"Order #{recentDispute.Id} requires attention",
                    Timestamp = recentDispute.OrderDate,
                    Icon = "bi-exclamation-triangle",
                    IconColor = "warning",
                    ActionUrl = Url.Action("OrderDetails", "Admin", new { id = recentDispute.Id }) ?? "#"
                });
            }

            // Add system backup activity
            var lastBackupTime = DateTime.UtcNow.AddHours(-8); // This should come from your actual backup system
            activities.Add(new SystemActivity
            {
                Title = "System backup completed",
                Description = "Daily backup successful",
                Timestamp = lastBackupTime,
                Icon = "bi-cloud-check",
                IconColor = "info",
                ActionUrl = "#"
            });

            // Get system health
            var activeSince = DateTime.UtcNow.AddMinutes(-15);
            var activeSessions = await _context.Users.CountAsync(u => u.LastSeen != null && u.LastSeen > activeSince);
            var systemHealth = new
            {
                ServerStatus = "Online",
                DatabaseStatus = "Connected",
                StorageUsage = 65,
                ActiveSessions = activeSessions,
                LastBackup = "8 hours ago"
            };

            // Get system alerts
            var systemAlerts = new List<SystemAlert>();

            // Check for pending product approvals
            var pendingProductsCount = await _context.Products.CountAsync(p => !p.IsApproved);
            if (pendingProductsCount > 0)
            {
                systemAlerts.Add(new SystemAlert
                {
                    Message = $"{pendingProductsCount} products pending approval",
                    Icon = "bi-exclamation-circle",
                    Severity = "warning",
                    ActionUrl = Url.Action("Products", "Admin", new { status = "pending" }) ?? "#"
                });
            }

            // Check for new merchant applications
            var pendingMerchantsCount = await _userManager.GetUsersInRoleAsync("Merchant")
                .ContinueWith(t => t.Result.Count(m => !m.EmailConfirmed));
            if (pendingMerchantsCount > 0)
            {
                systemAlerts.Add(new SystemAlert
                {
                    Message = $"{pendingMerchantsCount} new merchant applications",
                    Icon = "bi-person-plus",
                    Severity = "info",
                    ActionUrl = Url.Action("Merchants", "Admin", new { status = "pending" }) ?? "#"
                });
            }

            ViewBag.Stats = adminStats;
            ViewBag.SystemActivities = activities.OrderByDescending(a => a.Timestamp).ToList();
            ViewBag.SystemHealth = systemHealth;
            ViewBag.SystemAlerts = systemAlerts;

            return View();
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .Include(u => u.Faculty)
                .Include(u => u.AcademicYear)
                .ToListAsync();

            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    User = user,
                    Roles = roles.ToList()
                });
            }

            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.Faculties = await _context.Faculties.Select(f => f.Name).ToListAsync();
            ViewBag.AcademicYears = await _context.AcademicYears.Select(y => y.Year).ToListAsync();

            return View(userViewModels);
        }

        // Product Management for Admin
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Faculty)
                .Include(p => p.User)
                .Include(p => p.ProductImages) // <-- Add this line
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Get all faculties and merchants for filters
            ViewBag.Faculties = await _context.Faculties.ToListAsync();
            ViewBag.Merchants = await _userManager.GetUsersInRoleAsync("Merchant");

            return View(products);
        }

        // Product Approval
        public async Task<IActionResult> ApproveProduct(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                product.IsApproved = true;
#pragma warning disable CS8602 // Dereference of a possibly null reference.

                product.ApprovedBy = currentUser.Id;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                product.ApprovedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                // Notify merchant
                await _notificationService.CreateNotificationAsync(
                    product.UserId,
                    "Product Approved",
                    $"Your product '{product.Name}' has been approved.",
                    "System",
                    "/Merchant/Products");
                TempData["SuccessMessage"] = "Product approved successfully!";
            }
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        public async Task<IActionResult> RejectProduct(int productId, string reason)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.IsApproved = false;
                product.ApprovedBy = null;
                product.ApprovedAt = null;
                await _context.SaveChangesAsync();
                // Notify merchant
                await _notificationService.CreateNotificationAsync(
                    product.UserId,
                    "Product Rejected",
                    $"Your product '{product.Name}' was rejected. Reason: {reason}",
                    "Alert",
                    "/Merchant/Products");
                TempData["SuccessMessage"] = "Product rejected successfully!";
            }
            return RedirectToAction(nameof(Products));
        }

        // Orders Management for Admin
        public async Task<IActionResult> Orders(string status = null, string search = null, DateTime? date = null)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductImages)
                .AsQueryable();

            // Optional server-side filtering
            if (!string.IsNullOrEmpty(status))
                ordersQuery = ordersQuery.Where(o => o.ShippingStatus == status);
            if (!string.IsNullOrEmpty(search))
                ordersQuery = ordersQuery.Where(o => o.User.Email.Contains(search) || o.Id.ToString().Contains(search));
            if (date.HasValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate.Date == date.Value.Date);

            var orders = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(orders);
        }

        public class UpdateOrderStatusRequest
        {
            public int Id { get; set; }
            public string Status { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            // Manually validate anti-forgery token from header
            try
            {
                await _antiforgery.ValidateRequestAsync(HttpContext);
            }
            catch
            {
                return BadRequest(new { success = false, message = "Invalid anti-forgery token." });
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == request.Id);
            if (order != null)
            {
                order.ShippingStatus = request.Status;
                // Set DeliveryDate if status is Delivered
                if (request.Status == "Delivered")
                {
                    order.DeliveryDate = DateTime.UtcNow;
                }
                else
                {
                    order.DeliveryDate = null;
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // Merchant Management
        public async Task<IActionResult> Merchants()
        {
            var merchants = await _userManager.GetUsersInRoleAsync("Merchant");
            var merchantViewModels = new List<MerchantViewModel>();

            foreach (var merchant in merchants)
            {
                var performance = await CalculateMerchantPerformance(merchant);
                merchantViewModels.Add(new MerchantViewModel
                {
                    User = merchant,
                    Performance = performance
                });
            }

            return View(merchantViewModels);
        }

        // View Merchant Details
        public async Task<IActionResult> MerchantDetails(string id)
        {
            var merchant = await _userManager.FindByIdAsync(id);
            if (merchant == null)
            {
                TempData["ErrorMessage"] = "Merchant not found.";
                return RedirectToAction(nameof(Merchants));
            }

            var performance = await CalculateMerchantPerformance(merchant);

            // Get merchant's products
            var products = await _context.Products
                .Include(p => p.Faculty)
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Get merchant's recent orders
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == id))
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            // Get merchant's ratings
            var ratings = await _context.ProductRatings
                .Include(r => r.Product)
                .Include(r => r.User)
                .Where(r => r.Product.UserId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            var viewModel = new MerchantDetailViewModel
            {
                User = merchant,
                Performance = performance,
                RecentProducts = products,
                RecentOrders = recentOrders,
                RecentRatings = ratings
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveMerchant(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Update merchant status to active (not suspended)
                user.IsSuspended = false;
                await _userManager.UpdateAsync(user);

                // Reactivate their products
                var products = await _context.Products.Where(p => p.UserId == userId).ToListAsync();
                foreach (var product in products)
                {
                    if (product.StockQuantity == 0)
                    {
                        product.StockQuantity = 1; // Reactivate products with minimum stock
                    }
                }
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Merchant approved successfully!";
            }

            return RedirectToAction(nameof(Merchants));
        }

        [HttpPost]
        public async Task<IActionResult> SuspendMerchant(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Update merchant status
                user.IsSuspended = true;
                await _userManager.UpdateAsync(user);

                // Suspend all merchant's products
                var products = await _context.Products.Where(p => p.UserId == userId).ToListAsync();
                foreach (var product in products)
                {
                    product.StockQuantity = 0; // Effectively suspend by setting stock to 0
                }
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Merchant suspended and products deactivated!";
            }

            return RedirectToAction(nameof(Merchants));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent deleting yourself
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Error deleting user.";
            }

            return RedirectToAction(nameof(Users));
        }

        public IActionResult Categories()
        {
            return RedirectToAction("Index", "FacultyManagement");
        }

        public async Task<IActionResult> Analytics()
        {
            // Get sales data for the last 30 days
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var salesData = await _context.Orders
                .Where(o => o.OrderDate >= thirtyDaysAgo)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    date = g.Key,
                    revenue = g.SelectMany(o => o.OrderItems).Sum(oi => oi.Price * oi.Quantity),
                    orders = g.Count()
                })
                .OrderBy(x => x.date)
                .ToListAsync();

            // Get top products
            var topProductsRaw = await _context.OrderItems
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Faculty)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                .Where(oi => oi.Order.OrderDate >= thirtyDaysAgo)
                .GroupBy(oi => oi.Product)
                .Select(g => new
                {
                    Product = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Price * oi.Quantity)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

            // Ensure ProductImages are loaded for each product
            foreach (var item in topProductsRaw)
            {
                if (item.Product.ProductImages == null || !item.Product.ProductImages.Any())
                {
                    await _context.Entry(item.Product).Collection(p => p.ProductImages).LoadAsync();
                }
            }

            var topProducts = topProductsRaw;

            // Calculate sales metrics
            var totalOrders = await _context.Orders
                .Where(o => o.OrderDate >= thirtyDaysAgo)
                .CountAsync();

            var totalRevenue = await _context.OrderItems
                .Where(oi => oi.Order.OrderDate >= thirtyDaysAgo)
                .SumAsync(oi => oi.Price * oi.Quantity);

            var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            // Calculate inventory status
            var products = await _context.Products.ToListAsync();
            var inStockCount = products.Count(p => p.StockQuantity > 10);
            var lowStockCount = products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= 10);
            var outOfStockCount = products.Count(p => p.StockQuantity == 0);
            var totalProducts = products.Count;

            // Get current fee settings
            var settings = await _context.Settings.FirstOrDefaultAsync() ?? new Settings
            {
                MerchantFeePercentage = 5, // Default 5%
                UserFeePercentage = 2,     // Default 2%
                UserRewardPercentage = 1    // Default 1%
            };

            // Calculate real tax and shipping totals for all orders in the period
            decimal taxPercentage = settings.UserFeePercentage; // e.g. 14 for 14%
            decimal shippingAmount = settings.UserRewardPercentage; // e.g. 50 EGP per order
            var ordersWithItems = await _context.Orders
                .Where(o => o.OrderDate >= thirtyDaysAgo)
                .Include(o => o.OrderItems)
                .ToListAsync();
            decimal totalTaxCollected = 0;
            decimal totalShippingCollected = 0;
            foreach (var order in ordersWithItems)
            {
                decimal orderTotal = order.TotalAmount;
                totalTaxCollected += orderTotal * (taxPercentage / 100m);
                totalShippingCollected += shippingAmount;
            }

            // Display as summary on analytics page
            ViewBag.TotalTaxCollected = totalTaxCollected;
            ViewBag.TotalShippingCollected = totalShippingCollected;

            // Calculate platform growth (percentage change in revenue compared to previous period)
            var prevPeriodStart = thirtyDaysAgo.AddDays(-30);
            var prevPeriodEnd = thirtyDaysAgo;
            var prevRevenue = await _context.OrderItems
                .Where(oi => oi.Order.OrderDate >= prevPeriodStart && oi.Order.OrderDate < prevPeriodEnd)
                .SumAsync(oi => oi.Price * oi.Quantity);
            decimal growth = 0;
            if (prevRevenue > 0)
            {
                growth = ((totalRevenue - prevRevenue) / prevRevenue) * 100m;
            }
            else if (totalRevenue > 0)
            {
                growth = 100;
            }
            ViewBag.PlatformGrowth = growth;

            // Faculty Sales and Best Sellers
            var facultySalesRaw = await _context.Faculties
                .Select(f => new
                {
                    FacultyId = f.Id,
                    FacultyName = f.Name,
                    TotalRevenue = _context.OrderItems
                        .Where(oi => oi.Product.FacultyId == f.Id && oi.Order.OrderDate >= thirtyDaysAgo)
                        .Sum(oi => (decimal?)oi.Price * oi.Quantity) ?? 0
                })
                .ToListAsync();

            // Project to a list of Dictionary<string, object> for dynamic property assignment
            var facultySales = new List<Dictionary<string, object>>();
            foreach (var faculty in facultySalesRaw)
            {
                var dict = new Dictionary<string, object>
                {
                    ["FacultyId"] = faculty.FacultyId,
                    ["FacultyName"] = faculty.FacultyName,
                    ["TotalRevenue"] = faculty.TotalRevenue
                };
                var bestSellers = _context.OrderItems
                    .Where(oi => oi.Product.FacultyId == faculty.FacultyId && oi.Order.OrderDate >= thirtyDaysAgo)
                    .GroupBy(oi => oi.Product)
                    .Select(g => new
                    {
                        Product = g.Key,
                        ProductName = g.Key.Name,
                        Sold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Price * oi.Quantity)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(3)
                    .AsEnumerable()
                    .Select(x => new
                    {
                        x.ProductName,
                        x.Sold,
                        x.Revenue,
                        ImageUrl = x.Product.ProductImages.FirstOrDefault() != null ? x.Product.ProductImages.FirstOrDefault().ImageUrl : "/img/default-profile.png"
                    })
                    .ToList();
                dict["BestSellers"] = bestSellers;
                facultySales.Add(dict);
            }
            ViewBag.FacultySales = facultySales;

            ViewBag.SalesData = salesData;
            ViewBag.TopProducts = topProducts;
            ViewBag.AverageOrderValue = averageOrderValue;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.InventoryStatus = new
            {
                InStock = totalProducts > 0 ? (inStockCount * 100.0 / totalProducts) : 0,
                LowStock = totalProducts > 0 ? (lowStockCount * 100.0 / totalProducts) : 0,
                OutOfStock = totalProducts > 0 ? (outOfStockCount * 100.0 / totalProducts) : 0
            };

            return View();
        }

        private async Task<MerchantPerformanceViewModel> CalculateMerchantPerformance(ApplicationUser merchant)
        {
            // Get total products
            var productCount = await _context.Products
                .CountAsync(p => p.UserId == merchant.Id);

            // Get total orders
            var orderCount = await _context.Orders
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == merchant.Id))
                .CountAsync();

            // Calculate average rating
            var ratings = await _context.ProductRatings
                .Include(r => r.Product)
                .Where(r => r.Product.UserId == merchant.Id)
                .Select(r => r.Rating)
                .ToListAsync();

            var rating = ratings.Any() ? ratings.Average() : 0;

            // Calculate total revenue
            var totalRevenue = await _context.OrderItems
                .Where(oi => oi.Product.UserId == merchant.Id)
                .SumAsync(oi => oi.Quantity * oi.Price);

            // Calculate performance score (0-100)
            var performanceScore = CalculatePerformanceScore(productCount, orderCount, rating, totalRevenue);

            return new MerchantPerformanceViewModel
            {
                ProductCount = productCount,
                OrderCount = orderCount,
                Rating = (decimal)rating,
                TotalRevenue = totalRevenue,
                PerformanceScore = performanceScore
            };
        }

        private int CalculatePerformanceScore(int productCount, int orderCount, double rating, decimal revenue)
        {
            var score = 0;

            // For new merchants with no activity, return a baseline score
            if (productCount == 0 && orderCount == 0 && rating == 0 && revenue == 0)
            {
                return 0; // New merchant starts at 0%
            }

            // Products contribute up to 25 points
            score += Math.Min((productCount / 5) * 5, 25);

            // Orders contribute up to 25 points
            score += Math.Min((orderCount / 10) * 5, 25);

            // Rating contributes up to 25 points (5 points per star)
            score += (int)Math.Min((rating / 5) * 25, 25);

            // Revenue contributes up to 25 points (1 point per 200 in revenue)
            score += (int)Math.Min((revenue / 200) * 1, 25);

            return Math.Min(Math.Max(score, 0), 100); // Ensure score is between 0 and 100
        }

        // Merchants Management
        public async Task<IActionResult> Merchant()
        {
            var merchants = await _userManager.GetUsersInRoleAsync("Merchant");
            var merchantViewModels = new List<MerchantViewModel>();

            foreach (var merchant in merchants)
            {
                var performance = await CalculateMerchantPerformance(merchant);
                merchantViewModels.Add(new MerchantViewModel
                {
                    User = merchant,
                    Performance = performance
                });
            }

            return View(merchantViewModels);
        }

        [HttpGet]
        public async Task<IActionResult> GetMerchantDetails(string id)
        {
            var merchant = await _userManager.FindByIdAsync(id);
            if (merchant == null)
            {
                return NotFound();
            }

            var performance = await CalculateMerchantPerformance(merchant);

            // Get merchant's recent products
            var recentProducts = await _context.Products
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Get recent orders
            var recentOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == id))
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // Get recent activities
            var recentActivities = new List<SystemActivityViewModel>();
            
            // Add product activities
            var recentProductActivities = await _context.Products
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .Select(p => new SystemActivityViewModel
                {
                    Description = $"Added new product: {p.Name}",
                    CreatedAt = p.CreatedAt,
                    ActivityType = "Product",
                    IconClass = "bi-box-seam",
                    BadgeClass = "bg-primary"
                })
                .ToListAsync();
            recentActivities.AddRange(recentProductActivities);

            // Add order activities
            var recentOrderActivities = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.UserId == id)
                .OrderByDescending(oi => oi.Order.OrderDate)
                .Take(3)
                .Select(oi => new SystemActivityViewModel
                {
                    Description = $"Received order #{oi.Order.Id}",
                    CreatedAt = oi.Order.OrderDate,
                    ActivityType = "Order",
                    IconClass = "bi-bag",
                    BadgeClass = "bg-success"
                })
                .ToListAsync();
            recentActivities.AddRange(recentOrderActivities);

            var viewModel = new MerchantDetailViewModel
            {
                User = merchant,
                Performance = performance,
                RecentProducts = recentProducts,
                RecentOrders = recentOrders,
                RecentRatings = await _context.ProductRatings
                    .Include(r => r.Product)
                    .Include(r => r.User)
                    .Where(r => r.Product.UserId == id)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                RecentActivities = recentActivities.OrderByDescending(a => a.CreatedAt).Take(5).ToList()
            };

            return PartialView("_MerchantDetailsPartial", viewModel);
        }

        private async Task CreateMerchantNotification(ApplicationUser merchant, string title, string message, string? actionUrl)
        {
            var notification = new Notification
            {
                UserId = merchant.Id,
                Title = title,
                Message = message,
                Type = "MerchantStatus",
                ActionUrl = actionUrl
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // Revenue Management
        public async Task<IActionResult> Revenue()
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            // Get revenue data for the last 30 days
            var revenueData = await _context.Orders
                .Where(o => o.OrderDate >= thirtyDaysAgo)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    date = g.Key,
                    revenue = g.SelectMany(o => o.OrderItems).Sum(oi => oi.Price * oi.Quantity)
                })
                .OrderBy(x => x.date)
                .ToListAsync();

            // Get current fee settings
            var settings = await _context.Settings.FirstOrDefaultAsync() ?? new Settings
            {
                MerchantFeePercentage = 5, // Default 5%
                UserFeePercentage = 2,     // Default 2%
                UserRewardPercentage = 1    // Default 1%
            };

            // Calculate statistics
            var totalRevenue = await _context.OrderItems.SumAsync(oi => oi.Price * oi.Quantity);
            var merchantFees = totalRevenue * (settings.MerchantFeePercentage / 100);
            var userFees = totalRevenue * (settings.UserFeePercentage / 100);
            var userRewards = totalRevenue * (settings.UserRewardPercentage / 100);
            var netRevenue = totalRevenue + merchantFees + userFees - userRewards;

            ViewBag.MerchantFeePercentage = settings.MerchantFeePercentage;
            ViewBag.UserFeePercentage = settings.UserFeePercentage;
            ViewBag.UserRewardPercentage = settings.UserRewardPercentage;
            ViewBag.RevenueData = revenueData;
            ViewBag.TotalPlatformRevenue = totalRevenue;
            ViewBag.TotalMerchantFees = merchantFees;
            ViewBag.TotalUserFees = userFees;
            ViewBag.TotalUserRewards = userRewards;
            ViewBag.NetRevenue = netRevenue;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMerchantFee(decimal merchantFeePercentage)
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                settings = new Settings();
                _context.Settings.Add(settings);
            }

            settings.MerchantFeePercentage = merchantFeePercentage;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Merchant fee updated successfully.";
            return RedirectToAction(nameof(Revenue));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserFee(decimal userFeePercentage)
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                settings = new Settings();
                _context.Settings.Add(settings);
            }

            settings.UserFeePercentage = userFeePercentage;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "User fee updated successfully.";
            return RedirectToAction(nameof(Revenue));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserReward(decimal userRewardPercentage)
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                settings = new Settings();
                _context.Settings.Add(settings);
            }

            settings.UserRewardPercentage = userRewardPercentage;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "User reward updated successfully.";
            return RedirectToAction(nameof(Revenue));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdmin(string FullName, string Email, string Password)
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return RedirectToAction("Users");
            }

            var existingUser = await _userManager.FindByEmailAsync(Email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "A user with this email already exists.";
                return RedirectToAction("Users");
            }

            var names = FullName.Trim().Split(' ');
            var firstName = names.First();
            var lastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : "";

            var user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                FullName = FullName,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true, //auto-confirm admin emails
                ProfileImageUrl = "/img/default-profile.png" // Set default profile image
            };

            var result = await _userManager.CreateAsync(user, Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                TempData["SuccessMessage"] = "Admin user created successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join("<br>", result.Errors.Select(e => e.Description));
            }
            return RedirectToAction("Users");
        }

        // User Registration Notification (call this after user registration in AccountController)
        public async Task NotifyAdminsOfNewUser(ApplicationUser newUser)
        {
            var adminRole = await _roleManager.FindByNameAsync("Admin");
#pragma warning disable CS8602 // Dereference of a possibly null reference.

            var adminIds = await _context.UserRoles
                .Where(ur => ur.RoleId == adminRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            foreach (var adminId in adminIds)
            {
                await _notificationService.CreateNotificationAsync(
                    adminId,
                    "New User Registered",
                    $"A new user '{newUser.FullName}' has registered.",
                    "System",
                    "/Admin/Users");
            }
        }

        // Promotion/Offer Alert (broadcast)
        public async Task BroadcastPromotion(string title, string message, string? actionUrl = null)
        {
            var allUserIds = await _context.Users.Select(u => u.Id).ToListAsync();
            foreach (var userId in allUserIds)
            {
                await _notificationService.CreateNotificationAsync(
                    userId,
                    title,
                    message,
                    "System",
                    actionUrl ?? "/Offers");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Product not found.";
            }
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProductStatus(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Products));
            }

            // Toggle status: if active (StockQuantity > 0), deactivate (set StockQuantity = 0), else activate (set StockQuantity = 10 or 1)
            if (product.StockQuantity > 0)
            {
                product.StockQuantity = 0;
                TempData["SuccessMessage"] = "Product deactivated successfully!";
            }
            else
            {
                product.StockQuantity = 10; // Or set to 1 if you want to activate with minimum stock
                TempData["SuccessMessage"] = "Product activated successfully!";
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Products));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SupportTickets()
        {
            var tickets = await _context.SupportTickets
                .Include(t => t.User)
                .OrderByDescending(t => t.SentAt)
                .ToListAsync();
            return View(tickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTicketStatus(int ticketId, string status)
        {
            var ticket = await _context.SupportTickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(SupportTickets));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyToTicket(int ticketId, string message)
        {
            var ticket = await _context.SupportTickets.Include(t => t.Replies).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket != null && !string.IsNullOrWhiteSpace(message))
            {
                var reply = new SupportTicketReply
                {
                    SupportTicketId = ticketId,
                    Message = message,
                    SentAt = DateTime.UtcNow,
                    SenderRole = "Admin"
                };
                ticket.Replies.Add(reply);
                await _context.SaveChangesAsync();
                TempData["ReplySuccess"] = "Reply sent successfully!";
            }
            return RedirectToAction(nameof(SupportTickets));
        }

        [HttpGet]
        public async Task<IActionResult> ExportOrdersToExcel()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            // Get dynamic tax and shipping from settings
            var settings = await _context.Settings.FirstOrDefaultAsync();
            decimal taxPercentage = settings?.UserFeePercentage ?? 14; // fallback to 14 if not set
            decimal shippingAmount = settings?.UserRewardPercentage ?? 0; // fallback to 0 if not set
            var fileBytes = _excelExportService.ExportOrdersToExcel(orders, taxPercentage, shippingAmount);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Orders_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
        }
    }
}

