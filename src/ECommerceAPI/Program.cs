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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
	Console.WriteLine("[WARNING] DefaultConnection not found, using fallback");
	connectionString =
		"Server=DESKTOP-410L5DQ\\LOCALHOST;Database=ECommerceDB;User Id=sa;Password=Esoft@1234;TrustServerCertificate=True;Encrypt=False;";
}



builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));

// ================= JWT =================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

var secret = jwtSettings["Secret"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
{
	throw new Exception("JWT Secret not configured or too short");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = issuer,
		ValidAudience = audience,
		IssuerSigningKey = new SymmetricSecurityKey(
			Encoding.UTF8.GetBytes(secret)),
		ClockSkew = TimeSpan.Zero
	};
});

// ================= AMAZON =================
builder.Services.Configure<AmazonSettings>(
	builder.Configuration.GetSection("Amazon"));

builder.Services.AddHttpClient<AmazonTokenService>();
builder.Services.AddHttpClient<AmazonSpApiService>();

builder.Services.AddScoped<AmazonOAuthService>();
builder.Services.AddScoped<AmazonTokenService>();
builder.Services.AddScoped<AmazonSpApiService>();
builder.Services.AddScoped<AwsSigV4Signer>();

// ================= APP SERVICES =================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAmazonSellerService, AmazonSellerService>();
builder.Services.AddScoped<IAmazonBuyerService, AmazonBuyerService>();

// ================= CORS =================
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", p =>
		p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ================= SWAGGER =================
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
		Type = SecuritySchemeType.ApiKey,
		Scheme = "Bearer",
		In = ParameterLocation.Header,
		Description = "Bearer {token}"
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
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	db.Database.Migrate();
}

// ================= MIDDLEWARE =================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
	c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API v1");
	c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("API STARTED SUCCESSFULLY");
app.Run();
