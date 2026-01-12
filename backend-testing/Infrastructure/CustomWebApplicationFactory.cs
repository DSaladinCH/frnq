using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DSaladin.Frnq.Api;
using DSaladin.Frnq.Api.Quote.Providers;
using Moq;
using Microsoft.Extensions.Configuration;


namespace DSaladin.Frnq.Api.Testing.Infrastructure;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
	private readonly string _dbName = Guid.NewGuid().ToString();

	public Mock<IFinanceProvider> FinanceProviderMock { get; } = new();

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");
		builder.ConfigureAppConfiguration((context, config) =>
		{
			config.AddInMemoryCollection(new Dictionary<string, string?>
			{
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
			ServiceDescriptor? dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DatabaseContext));
			if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

			ServiceDescriptor? dbOptionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DatabaseContext>));
			if (dbOptionsDescriptor != null) services.Remove(dbOptionsDescriptor);

			// Add In-Memory Database
			services.AddDbContext<DatabaseContext>(options =>
			{
				options.UseInMemoryDatabase(_dbName);
				options.UseInternalServiceProvider(null);
			});

			// Mock FinanceProvider
			List<ServiceDescriptor> providerDescriptors = services.Where(d => d.ServiceType == typeof(IFinanceProvider)).ToList();
			foreach (ServiceDescriptor? d in providerDescriptors) services.Remove(d);

			FinanceProviderMock.Setup(p => p.InternalId).Returns("yahoo-finance");
			services.AddScoped(_ => FinanceProviderMock.Object);


		});
	}

	public void SeedData()
	{
		using IServiceScope scope = Services.CreateScope();
		IServiceProvider scopedServices = scope.ServiceProvider;
		DatabaseContext db = scopedServices.GetRequiredService<DatabaseContext>();
		db.Database.EnsureDeleted();
		db.Database.EnsureCreated();
		DataSeeder.Seed(db);
	}
}
