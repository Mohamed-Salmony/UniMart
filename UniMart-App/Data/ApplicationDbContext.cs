using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniMart_App.Models;

namespace UniMart_App.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductRating> ProductRatings { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Settings> Settings { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }
        public DbSet<SupportTicketReply> SupportTicketReplies { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure decimal precision for Settings
            builder.Entity<Settings>()
                .Property(s => s.MerchantFeePercentage)
                .HasPrecision(5, 2);

            builder.Entity<Settings>()
                .Property(s => s.UserFeePercentage)
                .HasPrecision(5, 2);

            builder.Entity<Settings>()
                .Property(s => s.UserRewardPercentage)
                .HasPrecision(5, 2);

            // Make Address and City nullable
            builder.Entity<ApplicationUser>()
                .Property(u => u.Address)
                .IsRequired(false);

            builder.Entity<ApplicationUser>()
                .Property(u => u.City)
                .IsRequired(false);

            // Update relationship configurations for nullable foreign keys
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Faculty)
                .WithMany(f => f.Users)
                .HasForeignKey(u => u.FacultyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.AcademicYear)
                .WithMany(a => a.Users)
                .HasForeignKey(u => u.AcademicYearId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<OrderItem>()
                .Property(o => o.Price)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.OriginalPrice)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.Rating)
                .HasPrecision(18, 2);

            // Configure Faculty-Product relationship explicitly
            builder.Entity<Product>()
                .HasOne(p => p.Faculty)
                .WithMany(f => f.Products)
                .HasForeignKey(p => p.FacultyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Product-User relationships
            builder.Entity<Product>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Product>()
                .HasOne(p => p.ApprovedByUser)
                .WithMany()
                .HasForeignKey(p => p.ApprovedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

