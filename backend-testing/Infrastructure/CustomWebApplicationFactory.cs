using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DSaladin.Frnq.Api;
using DSaladin.Frnq.Api.Quote.Providers;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;

namespace DSaladin.Frnq.Api.Testing.Infrastructure;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    public Mock<IFinanceProvider> FinanceProviderMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IsTesting"] = "true",
                ["JwtSettings:SecretKey"] = "SuperSecretKeyForTesting1234567890!",
                ["JwtSettings:Issuer"] = "frnq-api",
                ["JwtSettings:Audience"] = "frnq-ui",
                ["JwtSettings:AccessTokenExpiryInMinutes"] = "60",
                ["Features:SignupEnabled"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real DatabaseContext and its options
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DatabaseContext));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            var dbOptionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DatabaseContext>));
            if (dbOptionsDescriptor != null) services.Remove(dbOptionsDescriptor);

            // Add In-Memory Database
            services.AddDbContext<DatabaseContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.UseInternalServiceProvider(null);
            });

            // Mock FinanceProvider
            var providerDescriptors = services.Where(d => d.ServiceType == typeof(IFinanceProvider)).ToList();
            foreach (var d in providerDescriptors) services.Remove(d);

            FinanceProviderMock.Setup(p => p.InternalId).Returns("yahoo-finance");
            services.AddScoped(_ => FinanceProviderMock.Object);

            // Authentication override
            // We need to register authentication before adding our scheme, OR remove existing schemes
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
                options.DefaultScheme = "TestScheme";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
        });

        builder.UseEnvironment("Testing");
    }

    public void SeedData()
    {
        using var scope = Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var db = scopedServices.GetRequiredService<DatabaseContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        DataSeeder.Seed(db);
    }
}
