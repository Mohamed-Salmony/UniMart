using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Data;
using UniMart_App.Models;
using UniMart_App.Services;
using ClosedXML.Excel;
using System.IO;

namespace UniMart_App.Controllers
{
    [Authorize(Roles = "Merchant")]
    public class MerchantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ImageStorageService _imageStorageService;

        public MerchantController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ImageStorageService imageStorageService)
        {
            _context = context;
            _userManager = userManager;
            _imageStorageService = imageStorageService;
        }

        // Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            // Stats
            var merchantStats = new
            {
                TotalProducts = await _context.Products.CountAsync(p => p.UserId == userId),
                TotalOrders = await _context.Orders
                    .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId))
                    .CountAsync(),
                TotalRevenue = await _context.OrderItems
                    .Where(oi => oi.Product.UserId == userId)
                    .SumAsync(oi => oi.Quantity * oi.Price),
                LowStockProducts = await _context.Products
                    .Where(p => p.UserId == userId && p.StockQuantity < 10)
                    .CountAsync()
            };

            // Recent Activity
            var latestOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId))
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefaultAsync();
            var latestProductUpdate = await _context.Products
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .FirstOrDefaultAsync();
            var latestProductAdded = await _context.Products
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            // Performance Summary
            var salesThisMonth = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.UserId == userId && oi.Order.OrderDate >= monthStart)
                .SumAsync(oi => oi.Quantity * oi.Price);
            var ordersThisMonth = await _context.Orders
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId) && o.OrderDate >= monthStart)
                .CountAsync();
            var avgRating = await _context.ProductRatings
                .Where(r => r.Product.UserId == userId)
                .AverageAsync(r => (double?)r.Rating) ?? 0.0;

            ViewBag.Stats = merchantStats;
            ViewBag.LatestOrder = latestOrder;
            ViewBag.LatestProductUpdate = latestProductUpdate;
            ViewBag.LatestProductAdded = latestProductAdded;
            ViewBag.SalesThisMonth = salesThisMonth;
            ViewBag.OrdersThisMonth = ordersThisMonth;
            ViewBag.AvgRating = avgRating;
            return View();
        }

        // Product Management
        public async Task<IActionResult> Products()
        {
            var userId = _userManager.GetUserId(User);
            var products = await _context.Products
                .Include(p => p.Faculty)
                .Include(p => p.ProductImages) // Ensure only DB images are loaded
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // Create Product - GET
        public async Task<IActionResult> CreateProduct()
        {
            ViewBag.Faculties = await _context.Faculties.ToListAsync();
            return View();
        }

        // Create Product - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product, List<IFormFile> imageFiles)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
#pragma warning disable CS8601 // Possible null reference assignment.

                product.UserId = userId;
#pragma warning restore CS8601 // Possible null reference assignment.

                product.CreatedAt = DateTime.UtcNow;
                product.IsApproved = false; // Products need admin approval

                _context.Products.Add(product);
                await _context.SaveChangesAsync(); // Save first to get ProductId

                // Handle multiple image uploads using ProductImage entity
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    foreach (var imageFile in imageFiles)
                    {
                        if (imageFile != null && imageFile.Length > 0)
                        {
                            try
                            {
                                var imageUrl = await _imageStorageService.SaveImageAsync(imageFile);
#pragma warning disable CS8601 // Possible null reference assignment.

                                var productImage = new ProductImage
                                {
                                    ProductId = product.Id,
                                    ImageUrl = imageUrl,
                                    CreatedAt = DateTime.UtcNow
                                };
#pragma warning restore CS8601 // Possible null reference assignment.

                                _context.Add(productImage);
                            }
                            catch (Exception ex)
                            {
                                ModelState.AddModelError("", "Error uploading image: " + ex.Message);
                                ViewBag.Faculties = await _context.Faculties.ToListAsync();
                                return View(product);
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Product created successfully and is pending admin approval!";
                return RedirectToAction(nameof(Products));
            }

            ViewBag.Faculties = await _context.Faculties.ToListAsync();
            return View(product);
        }

        // Edit Product - GET
        public async Task<IActionResult> EditProduct(int id)
        {
            var userId = _userManager.GetUserId(User);
            var product = await _context.Products
                .Include(p => p.ProductImages) // Ensure images are loaded
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Faculties = await _context.Faculties.ToListAsync();
            return View(product);
        }

        // Edit Product - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product, IFormFile imageFile)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (existingProduct == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update product properties
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.OriginalPrice = product.OriginalPrice;
                    existingProduct.StockQuantity = product.StockQuantity;
                    existingProduct.Specifications = product.Specifications;
                    existingProduct.FacultyId = product.FacultyId;
                    existingProduct.UpdatedAt = DateTime.UtcNow;

                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _imageStorageService.SaveImageAsync(imageFile);
#pragma warning disable CS8601 // Possible null reference assignment.

                        var productImage = new ProductImage
                        {
                            ProductId = existingProduct.Id,
                            ImageUrl = imageUrl,
                            CreatedAt = DateTime.UtcNow
                        };
#pragma warning restore CS8601 // Possible null reference assignment.

                        _context.Add(productImage);
                    }

                    _context.Update(existingProduct);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Products));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating product: " + ex.Message);
                }
            }

            ViewBag.Faculties = await _context.Faculties.ToListAsync();
            return View(product);
        }

        // Delete Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var userId = _userManager.GetUserId(User);
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }

            return RedirectToAction(nameof(Products));
        }

        // Order Management
        public async Task<IActionResult> Orders()
        {
            var userId = _userManager.GetUserId(User);
            // Get merchant fee percentage from settings
            var settings = _context.Settings.FirstOrDefault();
            decimal merchantFeePercentage = settings?.MerchantFeePercentage ?? 0M;
            ViewBag.MerchantFeePercentage = merchantFeePercentage;

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Update Order Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && 
                    o.OrderItems.Any(oi => oi.Product.UserId == userId));

            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Order status updated successfully!";
            }

            return RedirectToAction(nameof(Orders));
        }

        // Analytics
        public async Task<IActionResult> Analytics()
        {
            var userId = _userManager.GetUserId(User);
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = monthStart.AddMonths(-1);
            var lastMonthEnd = monthStart.AddDays(-1);

            // Get sales data for the last 30 days
            var thirtyDaysAgo = now.AddDays(-30);
            var salesData = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.UserId == userId && oi.Order.OrderDate >= thirtyDaysAgo)
                .GroupBy(oi => oi.Order.OrderDate.Date)
                .Select(g => new
                {
                    date = g.Key,
                    revenue = g.Sum(oi => oi.Quantity * oi.Price),
                    orders = g.Count()
                })
                .OrderBy(x => x.date)
                .ToListAsync();

            // Get top selling products
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.UserId == userId)
                .GroupBy(oi => oi.Product)
                .Select(g => new
                {
                    Product = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.Price)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .ToListAsync();

            // Average Order Value (this month)
            var ordersThisMonth = await _context.Orders
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId) && o.OrderDate >= monthStart)
                .Include(o => o.OrderItems)
                .ToListAsync();
            decimal avgOrderValue = ordersThisMonth.Count > 0 ? ordersThisMonth.Average(o => o.OrderItems.Where(oi => oi.Product.UserId == userId).Sum(oi => oi.Quantity * oi.Price)) : 0;

            // Conversion Rate (if you track visits/sessions, otherwise set to 0)
            double conversionRate = 0; // Set to 0 or fetch from analytics if available

            // Return Rate (if you track returns, otherwise set to 0)
            double returnRate = 0; // Set to 0 or fetch from returns if available

            // Customer Satisfaction (average rating)
            double avgRating = await _context.ProductRatings
                .Where(r => r.Product.UserId == userId)
                .AverageAsync(r => (double?)r.Rating) ?? 0.0;

            // Inventory Status
            var allProducts = await _context.Products.Where(p => p.UserId == userId).ToListAsync();
            int totalProducts = allProducts.Count;
            int inStock = allProducts.Count(p => p.StockQuantity > 10);
            int lowStock = allProducts.Count(p => p.StockQuantity > 0 && p.StockQuantity <= 10);
            int outOfStock = allProducts.Count(p => p.StockQuantity == 0);
            int inStockPercent = totalProducts > 0 ? (int)Math.Round(inStock * 100.0 / totalProducts) : 0;
            int lowStockPercent = totalProducts > 0 ? (int)Math.Round(lowStock * 100.0 / totalProducts) : 0;
            int outOfStockPercent = totalProducts > 0 ? (int)Math.Round(outOfStock * 100.0 / totalProducts) : 0;

            // Inventory Health Score (simple logic)
            string inventoryScore = inStockPercent >= 80 ? "A+" : inStockPercent >= 60 ? "B" : "C";
            string inventoryHealthText = inStockPercent >= 80 ? "Excellent inventory management" : inStockPercent >= 60 ? "Good inventory management" : "Needs improvement";

            // Growth Trends (compare this month to last month)
            decimal revenueThisMonth = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.UserId == userId && oi.Order.OrderDate >= monthStart)
                .SumAsync(oi => oi.Quantity * oi.Price);
            decimal revenueLastMonth = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.UserId == userId && oi.Order.OrderDate >= lastMonthStart && oi.Order.OrderDate < monthStart)
                .SumAsync(oi => oi.Quantity * oi.Price);
            double revenueGrowth = revenueLastMonth > 0 ? (double)((revenueThisMonth - revenueLastMonth) / revenueLastMonth * 100) : 0;

            int ordersCountThisMonth = await _context.Orders
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId) && o.OrderDate >= monthStart)
                .CountAsync();
            int ordersCountLastMonth = await _context.Orders
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId) && o.OrderDate >= lastMonthStart && o.OrderDate < monthStart)
                .CountAsync();
            double orderGrowth = ordersCountLastMonth > 0 ? (double)(ordersCountThisMonth - ordersCountLastMonth) / ordersCountLastMonth * 100 : 0;

            // Customer Growth (if you track customers, otherwise set to 0)
            double customerGrowth = 0; // Set to 0 or fetch from analytics if available

            // Product Views (if you track, otherwise set to 0)
            double productViewsGrowth = 0; // Set to 0 or fetch from analytics if available

            ViewBag.SalesData = salesData;
            ViewBag.TopProducts = topProducts;
            ViewBag.AvgOrderValue = avgOrderValue;
            ViewBag.ConversionRate = conversionRate;
            ViewBag.ReturnRate = returnRate;
            ViewBag.AvgRating = avgRating;
            ViewBag.InStockPercent = inStockPercent;
            ViewBag.LowStockPercent = lowStockPercent;
            ViewBag.OutOfStockPercent = outOfStockPercent;
            ViewBag.InventoryScore = inventoryScore;
            ViewBag.InventoryHealthText = inventoryHealthText;
            ViewBag.RevenueGrowth = revenueGrowth;
            ViewBag.OrderGrowth = orderGrowth;
            ViewBag.CustomerGrowth = customerGrowth;
            ViewBag.ProductViewsGrowth = productViewsGrowth;
            return View();
        }

        // Revenue Page
        public async Task<IActionResult> Revenue()
        {
            var userId = _userManager.GetUserId(User);
            // Get merchant fee percentage from settings (default to 5% if not set)
            var settings = _context.Settings.FirstOrDefault();
            decimal merchantFeePercentage = settings?.MerchantFeePercentage ?? 0M;
            ViewBag.MerchantFeePercentage = merchantFeePercentage;

            var totalRevenue = await _context.OrderItems
                .Where(oi => oi.Product.UserId == userId)
                .SumAsync(oi => oi.Quantity * oi.Price);
            var recentOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId))
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            // Revenue chart data (last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var chartData = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.UserId == userId && oi.Order.OrderDate >= thirtyDaysAgo)
                .GroupBy(oi => oi.Order.OrderDate.Date)
                .Select(g => new {
                    Date = g.Key,
                    Revenue = g.Sum(oi => oi.Quantity * oi.Price)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.MerchantFeePercentage = merchantFeePercentage;
            ViewBag.ChartLabels = chartData.Select(x => x.Date.ToString("yyyy-MM-dd")).ToArray();
            ViewBag.ChartData = chartData.Select(x => x.Revenue).ToArray();
            return View();
        }

        // Inventory Page
        public async Task<IActionResult> Inventory()
        {
            var userId = _userManager.GetUserId(User);
            var allProducts = await _context.Products
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();
            return View(allProducts);
        }

        // Export Orders to Excel
        [HttpGet]
        public async Task<IActionResult> ExportOrdersToExcel()
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var settings = _context.Settings.FirstOrDefault();
            decimal merchantFeePercentage = settings?.MerchantFeePercentage ?? 0M;

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Orders");
                worksheet.Cell(1, 1).Value = "Order ID";
                worksheet.Cell(1, 2).Value = "Date";
                worksheet.Cell(1, 3).Value = "Items";
                worksheet.Cell(1, 4).Value = "Total";
                worksheet.Cell(1, 5).Value = "Platform Fee";
                worksheet.Cell(1, 6).Value = "Net Revenue";
                worksheet.Cell(1, 7).Value = "Product Names";

                int row = 2;
                foreach (var order in orders)
                {
                    decimal orderTotal = order.OrderItems.Sum(i => i.Quantity * i.Price);
                    decimal platformFee = orderTotal * merchantFeePercentage / 100M;
                    decimal netRevenue = orderTotal - platformFee;
                    string productNames = string.Join(", ", order.OrderItems.Select(i => i.Product.Name));

                    worksheet.Cell(row, 1).Value = order.Id;
                    worksheet.Cell(row, 2).Value = order.OrderDate.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(row, 3).Value = order.OrderItems.Count;
                    worksheet.Cell(row, 4).Value = orderTotal;
                    worksheet.Cell(row, 5).Value = platformFee;
                    worksheet.Cell(row, 6).Value = netRevenue;
                    worksheet.Cell(row, 7).Value = productNames;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "orders.xlsx");
                }
            }
        }

        // Export Top Products to Excel
        [HttpPost]
        public async Task<IActionResult> ExportTopProductsToExcel(int days = 30)
        {
            var userId = _userManager.GetUserId(User);
            var now = DateTime.UtcNow;
            var startDate = now.AddDays(-days);

            // Fetch top products for the period
            var topProductsRaw = await _context.OrderItems
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Faculty)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                .Where(oi => oi.Product.UserId == userId && oi.Order.OrderDate >= startDate)
                .ToListAsync();

            var settings = _context.Settings.FirstOrDefault();
            decimal merchantFeePercentage = settings?.MerchantFeePercentage ?? 0M;

            var topProducts = topProductsRaw
                .GroupBy(oi => oi.Product)
                .Select(g => new {
                    ProductName = g.Key.Name,
                    Faculty = g.Key.Faculty != null ? g.Key.Faculty.Name : null,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.Price),
                    NetRevenue = g.Sum(oi => oi.Quantity * oi.Price) * (1 - merchantFeePercentage / 100M)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("TopProducts");
                worksheet.Cell(1, 1).Value = "Product";
                worksheet.Cell(1, 2).Value = "Faculty";
                worksheet.Cell(1, 3).Value = "Sold";
                worksheet.Cell(1, 4).Value = "Revenue";
                worksheet.Cell(1, 5).Value = "Net Revenue";

                int row = 2;
                foreach (var item in topProducts)
                {
                    worksheet.Cell(row, 1).Value = item.ProductName;
                    worksheet.Cell(row, 2).Value = item.Faculty;
                    worksheet.Cell(row, 3).Value = item.TotalSold;
                    worksheet.Cell(row, 4).Value = item.Revenue;
                    worksheet.Cell(row, 5).Value = item.NetRevenue;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "top-products.xlsx");
                }
            }
        }

        // Get Analytics Data
        [HttpGet]
        public async Task<IActionResult> GetAnalyticsData(int days = 30)
        {
            var userId = _userManager.GetUserId(User);
            var now = DateTime.UtcNow;
            var startDate = now.AddDays(-days);

            // Sales Data
            var salesData = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.UserId == userId && oi.Order.OrderDate >= startDate)
                .GroupBy(oi => oi.Order.OrderDate.Date)
                .Select(g => new {
                    date = g.Key,
                    revenue = g.Sum(oi => oi.Quantity * oi.Price),
                    orders = g.Count()
                })
                .OrderBy(x => x.date)
                .ToListAsync();

            // Top Products (fetch to memory for image url)
            var topProductsRaw = await _context.OrderItems
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Faculty)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                .Where(oi => oi.Product.UserId == userId && oi.Order.OrderDate >= startDate)
                .ToListAsync();

            var settings = _context.Settings.FirstOrDefault();
            decimal merchantFeePercentage = settings?.MerchantFeePercentage ?? 0M;

            var topProducts = topProductsRaw
                .GroupBy(oi => oi.Product)
                .Select(g => new {
                    product = new {
                        name = g.Key.Name,
                        faculty = g.Key.Faculty != null ? g.Key.Faculty.Name : null,
                        imageUrl = g.Key.ProductImages.FirstOrDefault()?.ImageUrl
                    },
                    totalSold = g.Sum(oi => oi.Quantity),
                    revenue = g.Sum(oi => oi.Quantity * oi.Price),
                    netRevenue = g.Sum(oi => oi.Quantity * oi.Price) * (1 - merchantFeePercentage / 100M)
                })
                .OrderByDescending(x => x.totalSold)
                .Take(10)
                .ToList();

            // Metrics
            var ordersThisPeriod = await _context.Orders
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId) && o.OrderDate >= startDate)
                .Include(o => o.OrderItems)
                .ToListAsync();
            decimal avgOrderValue = ordersThisPeriod.Count > 0 ? ordersThisPeriod.Average(o => o.OrderItems.Where(oi => oi.Product.UserId == userId).Sum(oi => oi.Quantity * oi.Price)) : 0;
            double conversionRate = 0; // Set to 0 or fetch from analytics if available
            double returnRate = 0; // Set to 0 or fetch from returns if available
            double avgRating = await _context.ProductRatings
                .Where(r => r.Product.UserId == userId)
                .AverageAsync(r => (double?)r.Rating) ?? 0.0;

            // Inventory Status
            var allProducts = await _context.Products.Where(p => p.UserId == userId).ToListAsync();
            int totalProducts = allProducts.Count;
            int inStock = allProducts.Count(p => p.StockQuantity > 10);
            int lowStock = allProducts.Count(p => p.StockQuantity > 0 && p.StockQuantity <= 10);
            int outOfStock = allProducts.Count(p => p.StockQuantity == 0);
            int inStockPercent = totalProducts > 0 ? (int)Math.Round(inStock * 100.0 / totalProducts) : 0;
            int lowStockPercent = totalProducts > 0 ? (int)Math.Round(lowStock * 100.0 / totalProducts) : 0;
            int outOfStockPercent = totalProducts > 0 ? (int)Math.Round(outOfStock * 100.0 / totalProducts) : 0;
            string inventoryScore = inStockPercent >= 80 ? "A+" : inStockPercent >= 60 ? "B" : "C";
            string inventoryHealthText = inStockPercent >= 80 ? "Excellent inventory management" : inStockPercent >= 60 ? "Good inventory management" : "Needs improvement";

            // Growth Trends (compare this period to previous period)
            var prevStartDate = startDate.AddDays(-days);
            var prevEndDate = startDate;
            decimal revenueThisPeriod = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.UserId == userId && oi.Order.OrderDate >= startDate)
                .SumAsync(oi => oi.Quantity * oi.Price);
            decimal revenuePrevPeriod = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.UserId == userId && oi.Order.OrderDate >= prevStartDate && oi.Order.OrderDate < prevEndDate)
                .SumAsync(oi => oi.Quantity * oi.Price);
            double revenueGrowth = revenuePrevPeriod > 0 ? (double)((revenueThisPeriod - revenuePrevPeriod) / revenuePrevPeriod * 100) : 0;

            int ordersCountThisPeriod = await _context.Orders
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId) && o.OrderDate >= startDate)
                .CountAsync();
            int ordersCountPrevPeriod = await _context.Orders
                .Where(o => o.OrderItems.Any(oi => oi.Product.UserId == userId) && o.OrderDate >= prevStartDate && o.OrderDate < prevEndDate)
                .CountAsync();
            double orderGrowth = ordersCountPrevPeriod > 0 ? (double)(ordersCountThisPeriod - ordersCountPrevPeriod) / ordersCountPrevPeriod * 100 : 0;

            // Customer Growth (percentage change in new customers for the period vs previous period)
            int customersThisPeriod = await _context.Users.CountAsync(u => u.CreatedAt >= startDate && u.CreatedAt <= now);
            int customersPrevPeriod = await _context.Users.CountAsync(u => u.CreatedAt >= prevStartDate && u.CreatedAt < startDate);
            double customerGrowth = customersPrevPeriod > 0 ? (double)(customersThisPeriod - customersPrevPeriod) / customersPrevPeriod * 100 : 0;

            // Product Views (if you track, otherwise set to 0)
            double productViewsGrowth = 0; // Set to 0 or fetch from analytics if available

            // Revenue Rate (example: net revenue / gross revenue * 100)
            double revenueRate = 0;
            decimal grossRevenue = topProductsRaw.Sum(oi => oi.Quantity * oi.Price);
            decimal netRevenue = grossRevenue * (1 - merchantFeePercentage / 100M);
            if (grossRevenue > 0) revenueRate = (double)(netRevenue / grossRevenue * 100);

            return Json(new {
                salesData,
                topProducts,
                avgOrderValue,
                conversionRate,
                returnRate,
                avgRating,
                inStockPercent,
                lowStockPercent,
                outOfStockPercent,
                inventoryScore,
                inventoryHealthText,
                revenueGrowth,
                orderGrowth,
                customerGrowth,
                productViewsGrowth,
                revenueRate
            });
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}


