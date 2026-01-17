using ECommerceAPI.Amazon;
using ECommerceAPI.Data;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ================= BASIC DEBUG =================
Console.WriteLine("========================================");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine("========================================");

// ================= SERVICES =================
builder.Services.AddControllers();

// ================= DATABASE =================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? "Server=DESKTOP-410L5DQ\\LOCALHOST;Database=ECommerceDB;User Id=sa;Password=Esoft@1234;TrustServerCertificate=True;MultipleActiveResultSets=true";

// Register DbContext ONCE only
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));

Console.WriteLine($"[DB] Connection: {connectionString.Split(";")[0]}");

// ================= JWT =================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];

if (!string.IsNullOrEmpty(secretKey) && secretKey.Length >= 32)
{
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
			ValidIssuer = jwtSettings["Issuer"],
			ValidAudience = jwtSettings["Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
		};
	});
	Console.WriteLine("✅ JWT Authentication configured");
}
else
{
	Console.WriteLine("⚠️  JWT authentication not configured - skipping");
}

// ================= AMAZON SERVICES =================
// Register Amazon settings
builder.Services.Configure<AmazonSettings>(
	builder.Configuration.GetSection("Amazon"));

// Register HttpClients
builder.Services.AddHttpClient<AmazonTokenService>();
builder.Services.AddHttpClient<AmazonSpApiService>();

// Register Amazon services
builder.Services.AddScoped<AmazonOAuthService>();
builder.Services.AddScoped<AmazonTokenService>();
builder.Services.AddScoped<AmazonSpApiService>();
builder.Services.AddScoped<AwsSigV4Signer>();

Console.WriteLine("✅ Amazon services registered");

// ================= APP SERVICES =================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAmazonSellerService, AmazonSellerService>();
builder.Services.AddScoped<IAmazonBuyerService, AmazonBuyerService>();

// ================= SESSION =================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

// ================= CORS =================
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
		policy.AllowAnyOrigin()
			  .AllowAnyMethod()
			  .AllowAnyHeader());
});

// ================= SWAGGER =================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "E-Commerce API with Amazon Integration",
		Version = "v1",
		Description = "Complete E-Commerce API with Amazon Selling Partner API"
	});

	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.ApiKey,
		Scheme = "Bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "JWT Authorization header using Bearer scheme. Enter 'Bearer' [space] and then your token."
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

// ================= MIGRATION =================
try
{
	using var scope = app.Services.CreateScope();
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

	Console.WriteLine("[DB] Checking database...");

	if (db.Database.GetPendingMigrations().Any())
	{
		Console.WriteLine("[DB] Applying pending migrations...");
		db.Database.Migrate();
		Console.WriteLine("✅ Database migrations applied");
	}
	else
	{
		Console.WriteLine("✅ Database is up to date");
	}
}
catch (Exception ex)
{
	Console.WriteLine($"❌ Database error: {ex.Message}");
	Console.WriteLine("⚠️  Run 'Update-Database' in Package Manager Console");
}

// ================= MIDDLEWARE =================
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c =>
	{
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API v1");
		c.RoutePrefix = string.Empty; // Swagger at root
	});
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ================= ROOT ENDPOINT =================
app.MapGet("/health", () => Results.Ok(new
{
	status = "healthy",
	timestamp = DateTime.UtcNow,
	environment = app.Environment.EnvironmentName,
	database = "connected"
}));

Console.WriteLine("========================================");
Console.WriteLine("✅ API STARTED SUCCESSFULLY");
Console.WriteLine($"📍 Swagger: https://localhost:5261");
Console.WriteLine($"📍 Health: https://localhost:5261/health");
Console.WriteLine("========================================");

app.Run();