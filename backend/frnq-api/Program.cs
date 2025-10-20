using DSaladin.Frnq.Api;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Group;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Position;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Quote.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
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

// Add CORS policy for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
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
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCorsPolicy");
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
