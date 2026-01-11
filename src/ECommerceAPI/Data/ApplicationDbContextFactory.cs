using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerceAPI.Data;

public class ApplicationDbContextFactory
	: IDesignTimeDbContextFactory<ApplicationDbContext>
{
	public ApplicationDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

		optionsBuilder.UseSqlServer(
			"Server=DESKTOP-410L5DQ\\LOCALHOST;Database=ECommerceDB;User Id=sa;Password=Esoft@1234;TrustServerCertificate=True;"
		);

		return new ApplicationDbContext(optionsBuilder.Options);
	}
}
