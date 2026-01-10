namespace ECommerceAPI.Models;

public class OrderDto
{
	public int? Id { get; set; }
	public string? AmazonOrderId { get; set; }
	public string Status { get; set; } = string.Empty;
	public decimal TotalAmount { get; set; }
	public DateTime OrderDate { get; set; }
	public List<OrderItemDto> Items { get; set; } = new();
}
