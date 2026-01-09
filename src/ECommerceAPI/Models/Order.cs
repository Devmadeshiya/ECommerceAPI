using ECommerceAPI.src.ECommerceAPI.Models;
using System.ComponentModel.DataAnnotations;

public class Order
{
	[Key]
	public int Id { get; set; }

	public int BuyerId { get; set; }
	public User Buyer { get; set; } = null!;

	public string? AmazonOrderId { get; set; }

	[Required]
	public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled

	public decimal TotalAmount { get; set; }

	public DateTime OrderDate { get; set; } = DateTime.UtcNow;

	public DateTime? ShippedDate { get; set; }

	public DateTime? DeliveredDate { get; set; }

	public string? ShippingAddress { get; set; }

	public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}