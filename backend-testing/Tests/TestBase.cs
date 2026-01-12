using System.Diagnostics.CodeAnalysis;
using Allure.Xunit.Attributes.Steps;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using DSaladin.Frnq.Api.Quote.Providers;

[assembly: ExcludeFromCodeCoverage]

namespace DSaladin.Frnq.Api.Testing;

/// <summary>
/// Base class for integration tests that provides a test server with in-memory database and HttpClient.
/// </summary>
public abstract class TestBase : IDisposable
{
    public const LogLevel MIN_LOG_LEVEL = LogLevel.Warning;
    protected readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    protected HttpClient HttpClient { get; }
    protected DatabaseContext DbContext { get; }
    private AuthManagement AuthManagement { get; set; }
    protected ApiInterface ApiInterface { get; }
    protected Mock<IFinanceProvider> FinanceProviderMock { get; }
	
    [AllureBefore("Setup TestBase")]
    protected TestBase()
    {
        string dbName = Guid.NewGuid().ToString();

        FinanceProviderMock = new Mock<IFinanceProvider>();
        FinanceProviderMock.Setup(p => p.InternalId).Returns("yahoo-finance");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddFilter("Microsoft.EntityFrameworkCore.Update", MIN_LOG_LEVEL);
                    logging.AddFilter("Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware", LogLevel.None);
                    logging.AddConsole();
                    logging.SetMinimumLevel(MIN_LOG_LEVEL);
                });
                builder.ConfigureTestServices(services =>
                {
                    // Add in-memory database for testing
                    // actual database provider is not set if the environment is Testing
                    services.AddDbContext<DatabaseContext>(options =>
                    {
                        options.UseInMemoryDatabase(dbName);
                        options.LogTo(Console.WriteLine, MIN_LOG_LEVEL);
                    });

                    // Mock IFinanceProvider
                    List<ServiceDescriptor> providerDescriptors = services.Where(d => d.ServiceType == typeof(IFinanceProvider)).ToList();
                    foreach (ServiceDescriptor? d in providerDescriptors) services.Remove(d);
                    services.AddScoped(_ => FinanceProviderMock.Object);
                });

                builder.UseEnvironment("Testing");
            });

        HttpClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = false
        });

        ApiInterface = new ApiInterface(HttpClient);

        // Get DbContext from the factory for direct database access
        _scope = _factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        AuthManagement = _scope.ServiceProvider.GetRequiredService<AuthManagement>();

        var task = SeedDatabase(DbContext);
        task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Seeds the database with initial test data.
    /// Override this method in derived classes to provide custom test data.
    /// </summary>
    [AllureStep("Seed Initial Data")]
    protected static async Task SeedDatabase([Skip] DatabaseContext dbContext)
    {
		await DataSeeder.Seed(dbContext);
    }

    [AllureStep("Authenticate Test User")]
    protected async Task<AuthenticationScope<UserModel>> Authenticate()
    {
        UserModel user = await DataSeeder.GetTestUser(DbContext);
        return Authenticate(user);
    }

    [AllureStep("Authenticate Test User")]
    protected AuthenticationScope<UserModel> Authenticate(UserModel user)
    {
        string token = AuthManagement.GenerateAccessToken(user);
        HttpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return new AuthenticationScope<UserModel>(HttpClient, user);
    }

    [AllureStep("Clear Authentication")]
    protected void ClearAuthentication()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Represents an authentication scope that automatically clears authentication when disposed.
    /// </summary>
    protected class AuthenticationScope<UserModel> : IDisposable
    {
        private readonly HttpClient _httpClient;
        public UserModel User { get; }

        public AuthenticationScope(HttpClient httpClient, UserModel user)
        {
            _httpClient = httpClient;
            User = user;
        }

        public void Dispose()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    [AllureAfter("Dispose TestBase")]
    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        HttpClient.Dispose();
        _factory.Dispose();
        _scope.Dispose();
        GC.SuppressFinalize(this);
    }
}