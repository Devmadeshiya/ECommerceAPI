namespace ECommerceAPI.Models
{
	public class User
	{
		public int Id { get; set; }
		public int UserId => Id; // Alias for compatibility

		public string Email { get; set; } = string.Empty;
		public string PasswordHash { get; set; } = string.Empty;
		public string Role { get; set; } = "Buyer";
		public string UserType => Role; // Alias for compatibility

		public string FullName { get; set; } = string.Empty;
		public bool IsActive { get; set; } = true;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Navigation properties
		public SellerProfile? SellerProfile { get; set; }
		public ICollection<Order> Orders { get; set; } = new List<Order>();
	}
}