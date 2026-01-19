using ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Data;

public class ApplicationDbContext : DbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
		: base(options)
	{
	}

	public DbSet<User> Users { get; set; } = null!;
	public DbSet<SellerProfile> SellerProfiles { get; set; } = null!;
	public DbSet<Product> Products { get; set; } = null!;
	public DbSet<Order> Orders { get; set; } = null!;
	public DbSet<OrderItem> OrderItems { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// ==================== USER ====================
		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(u => u.Id);
			entity.HasIndex(u => u.Email).IsUnique();

			entity.Property(u => u.Email)
				.IsRequired()
				.HasMaxLength(256);

			entity.Property(u => u.Role)
				.IsRequired()
				.HasMaxLength(50);

			entity.Property(u => u.PasswordHash)
				.IsRequired()
				.HasMaxLength(500);

			entity.Property(u => u.FullName)
				.HasMaxLength(200);

			entity.Property(u => u.CreatedAt)
				.HasDefaultValueSql("GETUTCDATE()");
		});

		// ==================== SELLER PROFILE ====================
		modelBuilder.Entity<SellerProfile>(entity =>
		{
			entity.HasKey(sp => sp.Id);

			entity.HasIndex(sp => sp.UserId)
				.IsUnique();

			entity.Property(sp => sp.StoreName)
				.HasMaxLength(200);

			entity.Property(sp => sp.Description)
				.HasMaxLength(1000);

			entity.Property(sp => sp.AmazonRefreshToken)
				.HasMaxLength(500);

			entity.Property(sp => sp.AmazonSellerId)
				.HasMaxLength(100);

			entity.Property(sp => sp.CreatedAt)
				.HasDefaultValueSql("GETUTCDATE()");

			// User relationship
			entity.HasOne(sp => sp.User)
				.WithOne(u => u.SellerProfile)
				.HasForeignKey<SellerProfile>(sp => sp.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		// ==================== PRODUCT ====================
		modelBuilder.Entity<Product>(entity =>
		{
			entity.HasKey(p => p.Id);

			entity.HasIndex(p => p.ASIN);

			entity.Property(p => p.ASIN)
				.IsRequired()
				.HasMaxLength(20);

			entity.Property(p => p.Title)
				.IsRequired()
				.HasMaxLength(500);

			entity.Property(p => p.Description)
				.HasMaxLength(2000);

			entity.Property(p => p.Price)
				.HasPrecision(18, 2)
				.IsRequired();

			entity.Property(p => p.ImageUrl)
				.HasMaxLength(1000);

			entity.Property(p => p.Category)
				.HasMaxLength(100);

			entity.Property(p => p.CreatedAt)
				.HasDefaultValueSql("GETUTCDATE()");

			entity.Property(p => p.UpdatedAt)
				.HasDefaultValueSql("GETUTCDATE()");

			// Seller relationship
			entity.HasOne<SellerProfile>()
				.WithMany()
				.HasForeignKey(p => p.SellerId)
				.OnDelete(DeleteBehavior.Restrict)
				.IsRequired();
		});

		// ==================== ORDER ====================
		modelBuilder.Entity<Order>(entity =>
		{
			entity.HasKey(o => o.Id);

			entity.HasIndex(o => o.AmazonOrderId);

			entity.Property(o => o.AmazonOrderId)
				.HasMaxLength(50);

			entity.Property(o => o.Status)
				.IsRequired()
				.HasMaxLength(50)
				.HasDefaultValue("Pending");

			entity.Property(o => o.TotalAmount)
				.HasPrecision(18, 2)
				.IsRequired();

			entity.Property(o => o.OrderDate)
				.HasDefaultValueSql("GETUTCDATE()");

			// Buyer relationship
			entity.HasOne(o => o.Buyer)
				.WithMany(u => u.Orders)
				.HasForeignKey(o => o.BuyerId)
				.OnDelete(DeleteBehavior.Restrict);

			// Order items relationship
			entity.HasMany(o => o.OrderItems)
				.WithOne(oi => oi.Order)
				.HasForeignKey(oi => oi.OrderId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		// ==================== ORDER ITEM ====================
		modelBuilder.Entity<OrderItem>(entity =>
		{
			entity.HasKey(oi => oi.Id);

			entity.Property(oi => oi.ASIN)
				.IsRequired()
				.HasMaxLength(20);

			entity.Property(oi => oi.ProductTitle)
				.IsRequired()
				.HasMaxLength(500);

			entity.Property(oi => oi.UnitPrice)
				.HasPrecision(18, 2)
				.IsRequired();

			entity.Property(oi => oi.TotalPrice)
				.HasPrecision(18, 2)
				.IsRequired();

			entity.Property(oi => oi.Quantity)
				.IsRequired();
		});

		// ==================== INDEXES FOR PERFORMANCE ====================
		modelBuilder.Entity<Product>()
			.HasIndex(p => new { p.SellerId, p.IsActive });

		modelBuilder.Entity<Order>()
			.HasIndex(o => new { o.BuyerId, o.OrderDate });

		modelBuilder.Entity<SellerProfile>()
			.HasIndex(sp => sp.IsAmazonConnected);
	}
}