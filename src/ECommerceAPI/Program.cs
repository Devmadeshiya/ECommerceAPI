using ECommerceAPI.Data;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];

// Debug output
Console.WriteLine($"[DEBUG] JWT Secret exists: {!string.IsNullOrEmpty(secretKey)}");
Console.WriteLine($"[DEBUG] JWT Issuer: {jwtSettings["Issuer"]}");
Console.WriteLine($"[DEBUG] JWT Audience: {jwtSettings["Audience"]}");

// Handle missing JWT Secret
if (string.IsNullOrEmpty(secretKey))
{
	Console.WriteLine("[WARNING] JWT Secret not found in appsettings.json!");
	Console.WriteLine("[WARNING] Using default secret for DEVELOPMENT only. DO NOT use in production!");
	secretKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!@#$%";
}

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = jwtSettings["Issuer"] ?? "ECommerceAPI",
		ValidAudience = jwtSettings["Audience"] ?? "ECommerceClient",
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
		ClockSkew = TimeSpan.Zero
	};
});

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAmazonSellerService, AmazonSellerService>();
builder.Services.AddScoped<IAmazonBuyerService, AmazonBuyerService>();

// Configure CORS
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		policy.AllowAnyOrigin()
			  .AllowAnyMethod()
			  .AllowAnyHeader();
	});
});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "E-Commerce API with Amazon Integration",
		Version = "v1",
		Description = "API for managing sellers and buyers with Amazon SP-API and PAAPI integration"
	});

	// Add JWT Authentication to Swagger
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.ApiKey,
		Scheme = "Bearer"
	});

	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});
});

var app = builder.Build();

// Apply migrations and seed database
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		var context = services.GetRequiredService<ApplicationDbContext>();
		context.Database.Migrate();
		Console.WriteLine("[SUCCESS] Database migration completed successfully.");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"[ERROR] An error occurred while migrating the database: {ex.Message}");
	}
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c =>
	{
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API V1");
		c.RoutePrefix = string.Empty;
	});
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine("========================================");
Console.WriteLine("E-Commerce API is running...");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine("Swagger UI: https://localhost:7049");
Console.WriteLine("API Base: https://localhost:7049/api");
Console.WriteLine("========================================");

app.Run();