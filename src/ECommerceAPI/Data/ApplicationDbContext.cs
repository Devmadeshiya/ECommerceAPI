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

		// User configuration
		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(u => u.Id);
			entity.HasIndex(u => u.Email).IsUnique();
			entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
			entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
			entity.Property(u => u.PasswordHash).IsRequired();
		});

		// SellerProfile configuration
		modelBuilder.Entity<SellerProfile>(entity =>
		{
			entity.HasKey(sp => sp.Id);
			entity.HasOne(sp => sp.User)
				.WithOne(u => u.SellerProfile)
				.HasForeignKey<SellerProfile>(sp => sp.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		// Product configuration
		modelBuilder.Entity<Product>(static entity =>
		{
			entity.HasKey(p => p.Id);
			entity.HasIndex(p => p.ASIN);
			entity.Property(p => p.ASIN).IsRequired().HasMaxLength(20);
			entity.Property(p => p.Title).IsRequired().HasMaxLength(500);
			entity.Property(p => p.Price).HasPrecision(18, 2);

			entity.HasOne<SellerProfile>()
				.WithMany()
				.HasForeignKey(p => p.SellerId)
				.OnDelete(DeleteBehavior.SetNull);
		});

		// Order configuration
		modelBuilder.Entity<Order>(entity =>
		{
			entity.HasKey(o => o.Id);
			entity.Property(o => o.Status).IsRequired().HasMaxLength(50);
			entity.Property(o => o.TotalAmount).HasPrecision(18, 2);

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
			entity.HasKey(oi => oi.Id);
			entity.Property(oi => oi.ASIN).IsRequired().HasMaxLength(20);
			entity.Property(oi => oi.ProductTitle).IsRequired().HasMaxLength(500);
			entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
			entity.Property(oi => oi.TotalPrice).HasPrecision(18, 2);
		});
	}
}