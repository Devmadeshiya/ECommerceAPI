using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

public class OrderItem
{
	[Key]
	public int Id { get; set; }

	public int OrderId { get; set; }
	public Order Order { get; set; } = null!;

	public string ASIN { get; set; } = string.Empty;
	public string ProductTitle { get; set; } = string.Empty;

	public decimal UnitPrice { get; set; }
	public int Quantity { get; set; }
	public decimal TotalPrice { get; set; }
}
