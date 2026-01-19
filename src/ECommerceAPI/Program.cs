using ECommerceAPI.Amazon;
using ECommerceAPI.Data;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("========================================");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"ContentRoot: {builder.Environment.ContentRootPath}");
Console.WriteLine("========================================");

// --------------------------------------------------
// Controllers
// --------------------------------------------------
builder.Services.AddControllers();

// --------------------------------------------------
// Database
// --------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//if (string.IsNullOrWhiteSpace(connectionString))
//{
//	throw new Exception("❌ Connection string 'DefaultConnection' not found in appsettings.json");
//}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
	options.UseSqlServer(connectionString);
	options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

Console.WriteLine("✅ Database configured");

// --------------------------------------------------
// JWT AUTHENTICATION (FINAL)
// --------------------------------------------------
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

var secretKey = jwtSettings["Secret"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
	throw new Exception("❌ JwtSettings:Secret missing or too short");

if (string.IsNullOrWhiteSpace(issuer))
	throw new Exception("❌ JwtSettings:Issuer missing");

if (string.IsNullOrWhiteSpace(audience))
	throw new Exception("❌ JwtSettings:Audience missing");

Console.WriteLine($"[JWT] Secret Length: {secretKey.Length}");
Console.WriteLine($"[JWT] Issuer: {issuer}");
Console.WriteLine($"[JWT] Audience: {audience}");

builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.RequireHttpsMetadata = false;
		options.SaveToken = true;

		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,

			ValidIssuer = issuer,
			ValidAudience = audience,

			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(secretKey)
			),

			ClockSkew = TimeSpan.Zero
		};

		options.Events = new JwtBearerEvents
		{
			OnAuthenticationFailed = context =>
			{
				Console.WriteLine($"❌ JWT Auth Failed: {context.Exception.Message}");
				return Task.CompletedTask;
			},
			OnTokenValidated = context =>
			{
				var userId = context.Principal?
					.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

				var role = context.Principal?
					.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

				Console.WriteLine($"✅ JWT Validated | UserId: {userId}, Role: {role}");
				return Task.CompletedTask;
			}
		};
	});

builder.Services.AddAuthorization();
Console.WriteLine("✅ JWT Authentication configured");

// --------------------------------------------------
// Amazon Services
// --------------------------------------------------
builder.Services.Configure<AmazonSettings>(
	builder.Configuration.GetSection("AmazonSettings"));

builder.Services.AddHttpClient<AmazonTokenService>();
builder.Services.AddHttpClient<AmazonSpApiService>();

builder.Services.AddScoped<AmazonOAuthService>();
builder.Services.AddScoped<AmazonTokenService>();
builder.Services.AddScoped<AmazonSpApiService>();
builder.Services.AddScoped<AwsSigV4Signer>();

Console.WriteLine("✅ Amazon services registered");

// --------------------------------------------------
// Application Services
// --------------------------------------------------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAmazonSellerService, AmazonSellerService>();
builder.Services.AddScoped<IAmazonBuyerService, AmazonBuyerService>();

Console.WriteLine("✅ Application services registered");

// --------------------------------------------------
// Session & CORS
// --------------------------------------------------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
		policy.AllowAnyOrigin()
			  .AllowAnyMethod()
			  .AllowAnyHeader());
});

// --------------------------------------------------
// Swagger
// --------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "E-Commerce API",
		Version = "v1"
	});

	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "Bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "Paste JWT token only"
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

// --------------------------------------------------
// Middleware
// --------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
	c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API v1");
	c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
	status = "healthy",
	time = DateTime.UtcNow
}));

Console.WriteLine("========================================");
Console.WriteLine("✅ API STARTED SUCCESSFULLY");
Console.WriteLine("📍 Swagger: https://localhost:7050");
Console.WriteLine("========================================");

app.Run();
