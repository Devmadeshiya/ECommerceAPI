using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ECommerceAPI.Data
{
	public class ApplicationDbContextFactory
		: IDesignTimeDbContextFactory<ApplicationDbContext>
	{
		public ApplicationDbContext CreateDbContext(string[] args)
		{
			var config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false)
				.AddJsonFile("appsettings.Development.json", optional: true)
				.Build();

			var connectionString = config.GetConnectionString("DefaultConnection");

			var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
			optionsBuilder.UseSqlServer(connectionString);

			return new ApplicationDbContext(optionsBuilder.Options);
		}
	}
}
