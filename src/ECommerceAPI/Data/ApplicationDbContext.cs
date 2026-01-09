using ECommerceAPI.Models;
using ECommerceAPI.src.ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Data;

public class ApplicationDbContext : DbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
		: base(options)
	{
	}

	public DbSet<User> Users { get; set; }
	public DbSet<SellerProfile> SellerProfiles { get; set; }
	public DbSet<Product> Products { get; set; }
	public DbSet<Order> Orders { get; set; }
	public DbSet<OrderItem> OrderItems { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// User configuration
		modelBuilder.Entity<User>(entity =>
		{
			entity.HasIndex(e => e.Email).IsUnique();
			entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
			entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
		});

		// SellerProfile configuration
		modelBuilder.Entity<SellerProfile>(entity =>
		{
			entity.HasOne(sp => sp.User)
				.WithOne(u => u.SellerProfile)
				.HasForeignKey<SellerProfile>(sp => sp.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		// Product configuration
		modelBuilder.Entity<Product>(entity =>
		{
			entity.HasIndex(e => e.ASIN);
			entity.Property(e => e.Price).HasPrecision(18, 2);

			entity.HasOne(p => p.Seller)
				.WithMany()
				.HasForeignKey(p => p.SellerId)
				.OnDelete(DeleteBehavior.SetNull);
		});

		// Order configuration
		modelBuilder.Entity<Order>(entity =>
		{
			entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

			entity.HasOne(o => o.Buyer)
				.WithMany(u => u.Orders)
				.HasForeignKey(o => o.BuyerId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasMany(o => o.OrderItems)
				.WithOne(oi => oi.Order)
				.HasForeignKey(oi => oi.OrderId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		// OrderItem configuration
		modelBuilder.Entity<OrderItem>(entity =>
		{
			entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
			entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
		});
	}
}