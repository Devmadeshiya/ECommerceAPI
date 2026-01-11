using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

public class Order
{
	[Key]
	public int Id { get; set; }

	public int BuyerId { get; set; }
	public User Buyer { get; set; } = null!;

	public string? AmazonOrderId { get; set; }

	public string Status { get; set; } = "Pending";

	public decimal TotalAmount { get; set; }

	public DateTime OrderDate { get; set; } = DateTime.UtcNow;

	public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
