namespace ECommerceAPI.Models;

public class OrderItemDto
{
	public string ASIN { get; set; } = string.Empty;
	public string ProductTitle { get; set; } = string.Empty;
	public decimal UnitPrice { get; set; }
	public int Quantity { get; set; }
	public decimal TotalPrice { get; set; }
}
