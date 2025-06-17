using DSaladin.Frnq.Api;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Position;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Quote.Providers;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddScoped<YahooFinanceProvider>();
builder.Services.AddScoped<DatabaseProvider>();
builder.Services.AddScoped<IFinanceProvider, YahooFinanceProvider>();
builder.Services.AddScoped<InvestmentManagement>();
builder.Services.AddScoped<PositionManagement>();
builder.Services.AddScoped<QuoteManagement>();

// Add CORS policy for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
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

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
