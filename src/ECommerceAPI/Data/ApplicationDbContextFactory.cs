using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerceAPI.Data
{
	public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
	{
		public ApplicationDbContext CreateDbContext(string[] args)
		{
			// Hardcoded connection string for migrations
			// This is only used during design-time (migrations)
			var connectionString = "Server=DESKTOP-410L5DQ\\LOCALHOST;Database=ECommerceDB;User Id=sa;Password=Esoft@1234;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true";

			var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
			optionsBuilder.UseSqlServer(connectionString);

			Console.WriteLine("[MIGRATION] Using connection: " + connectionString);

			return new ApplicationDbContext(optionsBuilder.Options);
		}
	}
}