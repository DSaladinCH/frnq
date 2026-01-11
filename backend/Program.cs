using DSaladin.Frnq.Api;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Group;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Position;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Quote.Providers;
using DSaladin.Frnq.Api.Result;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
	});
	
builder.Services.AddScoped<InvestmentManagement>();
builder.Services.AddScoped<PositionManagement>();
builder.Services.AddScoped<QuoteManagement>();
builder.Services.AddScoped<GroupManagement>();
builder.Services.AddScoped<AuthManagement>();
builder.Services.AddScoped<OidcManagement>();

// Add HTTP context accessor for AuthManagement
builder.Services.AddHttpContextAccessor();

// Add OIDC initialization service
builder.Services.AddHostedService<OidcInitializationService>();

builder.Services.AddScoped<DatabaseProvider>();
builder.Services.AddScoped<YahooFinanceProvider>();
builder.Services.AddScoped<IFinanceProvider, YahooFinanceProvider>();
builder.Services.AddScoped<ProviderRegistry>();

// Configure automatic model validation
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
	options.InvalidModelStateResponseFactory = context =>
	{
		ModelError? firstError = context.ModelState
			.FirstOrDefault(e => e.Value?.Errors.Count > 0)
			.Value?.Errors.FirstOrDefault();

		if (firstError?.ErrorMessage != null && firstError.ErrorMessage.Contains('|'))
		{
			// Parse ResponseCode format: "CODE|Description"
			var parts = firstError.ErrorMessage.Split('|', 2);
			var codeDescription = new CodeDescriptionModel(parts[0], parts[1]);
			return ApiResponse.Create(codeDescription, System.Net.HttpStatusCode.BadRequest).Response;
		}

		// Fallback to generic validation error
		var message = firstError?.ErrorMessage ?? "Validation failed";
		return ApiResponse.Create("VALIDATION_FAILED", message, System.Net.HttpStatusCode.BadRequest).Response;
	};
});

// Add JWT authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuerSigningKey = true,
		IssuerSigningKey = new SymmetricSecurityKey(key),
		ValidateIssuer = true,
		ValidIssuer = jwtSettings["Issuer"],
		ValidateAudience = true,
		ValidAudience = jwtSettings["Audience"],
		ValidateLifetime = true,
		ClockSkew = TimeSpan.Zero
	};
});

// Add CORS policy with configurable origins
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
	options.AddPolicy("AppCorsPolicy", policy =>
	{
		policy.WithOrigins(allowedOrigins)
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials(); // Required for cookies
	});
});

string connectionString = builder.Configuration.GetConnectionString("DatabaseConnection") ?? throw new InvalidOperationException("Connection string 'DatabaseConnection' not found.");
builder.Services.AddDbContext<DatabaseContext>(options =>
	options.UseNpgsql(connectionString));


var app = builder.Build();

using (var localScope = app.Services.CreateScope())
{
	DatabaseContext databaseContext = localScope.ServiceProvider.GetRequiredService<DatabaseContext>();
	databaseContext.Database.Migrate();
}


// Configure the HTTP request pipeline.
app.UseCors("AppCorsPolicy");

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
