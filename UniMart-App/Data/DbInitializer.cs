using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniMart_App.Models;

namespace UniMart_App.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            try
            {
                // Check if we can connect to the database
                if (!await context.Database.CanConnectAsync())
                {
                    throw new Exception("Cannot connect to the database. Please check your connection string.");
                }

                // Create roles regardless of migrations
                await CreateRolesAsync(roleManager);

                // Seed Faculties if needed
                await SeedFacultiesAsync(context);

                // Seed Academic Years if needed
                await SeedAcademicYearsAsync(context);

                // Create admin user if needed
                await CreateAdminUserAsync(userManager, context);

                // Create demo merchant user if needed
                var merchantUser = await CreateMerchantUserAsync(userManager, context);

                // Seed sample products if needed
                await SeedSampleProductsAsync(context, merchantUser);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while seeding the database: {ex.Message}", ex);
            }
        }

        private static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "User", "Merchant" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to create role {role}: {errors}");
                    }
                }
            }
        }

        private static async Task SeedFacultiesAsync(ApplicationDbContext context)
        {
            if (!await context.Faculties.AnyAsync())
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

                try
                {
                    await context.Faculties.AddRangeAsync(faculties);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error seeding faculties: {ex.Message}", ex);
                }
            }
        }

        private static async Task SeedAcademicYearsAsync(ApplicationDbContext context)
        {
            if (!await context.AcademicYears.AnyAsync())
            {
                var academicYears = new[]
                {
                    new AcademicYear { Year = "First Year" },
                    new AcademicYear { Year = "Second Year" },
                    new AcademicYear { Year = "Third Year" },
                    new AcademicYear { Year = "Fourth Year" },
                    new AcademicYear { Year = "Fifth Year" },
                    new AcademicYear { Year = "Sixth Year" },
                    new AcademicYear { Year = "Seventh Year" }
                };

                try
                {
                    await context.AcademicYears.AddRangeAsync(academicYears);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error seeding academic years: {ex.Message}", ex);
                }
            }
        }

        private static async Task CreateAdminUserAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            var adminEmail = "admin@unimart.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Admin User",
                    FirstName = "Admin",
                    LastName = "User",
                    ProfileImageUrl = "https://ui-avatars.com/api/?name=Admin+User&background=random"
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create admin user: {errors}");
                }
            }
        }

        private static async Task<ApplicationUser> CreateMerchantUserAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            var merchantEmail = "merchant@unimart.com";
            var merchantUser = await userManager.FindByEmailAsync(merchantEmail);

            if (merchantUser != null)
            {
                return merchantUser;
            }

            var merchant = new ApplicationUser
            {
                UserName = merchantEmail,
                Email = merchantEmail,
                EmailConfirmed = true,
                FullName = "Demo Merchant",
                FirstName = "Demo",
                LastName = "Merchant",
                ProfileImageUrl = "https://ui-avatars.com/api/?name=Demo+Merchant&background=random"
            };

            var result = await userManager.CreateAsync(merchant, "Merchant123!");
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create merchant user: {errors}");
            }

            await userManager.AddToRoleAsync(merchant, "Merchant");
            return merchant;
        }

        private static async Task SeedSampleProductsAsync(ApplicationDbContext context, ApplicationUser merchantUser)
        {
            if (!await context.Products.AnyAsync())
            {
                var faculty = await context.Faculties.FirstOrDefaultAsync();
                if (faculty != null && merchantUser != null)
                {
                    var sampleProduct = new Product
                    {
                        Name = "Sample Engineering Textbook",
                        Description = "A comprehensive textbook for engineering students",
                        Price = 299.99m,
                        StockQuantity = 10,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow,
                        FacultyId = faculty.Id,
                        UserId = merchantUser.Id
                    };

                    await context.Products.AddAsync(sampleProduct);
                    await context.SaveChangesAsync();

                    // Add sample image
                    var sampleImage = new ProductImage
                    {
                        ProductId = sampleProduct.Id,
                        ImageUrl = "/product_images/sample-textbook.jpg",
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.AddAsync(sampleImage);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}

