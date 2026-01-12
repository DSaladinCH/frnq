using System.Diagnostics.CodeAnalysis;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using DSaladin.Frnq.Api;
using Microsoft.Extensions.DependencyInjection;

[assembly: ExcludeFromCodeCoverage]

namespace DSaladin.Frnq.Api.Testing.Tests;

public abstract class BaseTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    protected readonly CustomWebApplicationFactory<Program> Factory;
    protected readonly ApiInterface Api;
    protected readonly HttpClient HttpClient;

    protected BaseTest(CustomWebApplicationFactory<Program> factory)
    {
        Factory = factory;
        HttpClient = factory.CreateClient();
        Api = new ApiInterface(HttpClient);
        Factory.SeedData();
    }

    protected DatabaseContext GetDatabaseContext()
    {
		IServiceScope scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    }

    /// <summary>
    /// Executes a function within a database context scope, automatically disposing the scope.
    /// Use this method for database operations to ensure proper resource cleanup.
    /// </summary>
    protected T ExecuteWithDatabaseContext<T>(Func<DatabaseContext, T> action)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
		DatabaseContext context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        return action(context);
    }

    /// <summary>
    /// Executes an async function within a database context scope, automatically disposing the scope.
    /// Use this method for database operations to ensure proper resource cleanup.
    /// </summary>
    protected async Task<T> ExecuteWithDatabaseContextAsync<T>(Func<DatabaseContext, Task<T>> action)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
		DatabaseContext context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        return await action(context);
    }

    /// <summary>
    /// Executes an action within a database context scope, automatically disposing the scope.
    /// Use this method for database operations to ensure proper resource cleanup.
    /// </summary>
    protected void ExecuteWithDatabaseContext(Action<DatabaseContext> action)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
		DatabaseContext context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        action(context);
    }

    /// <summary>
    /// Executes an async action within a database context scope, automatically disposing the scope.
    /// Use this method for database operations to ensure proper resource cleanup.
    /// </summary>
    protected async Task ExecuteWithDatabaseContextAsync(Func<DatabaseContext, Task> action)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
		DatabaseContext context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        await action(context);
    }

    protected async Task AuthenticateAsync()
    {
		TestResponse<AuthResponseDto> loginResponse = await Api.Auth.Login(new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        });

        if (!loginResponse.Success || loginResponse.Content?.AccessToken == null)
        {
            throw new InvalidOperationException(
                $"Authentication failed. Login returned {loginResponse.StatusCode}. " +
                $"Error: {loginResponse.Error?.Description ?? "No error message"}");
        }

        Api.SetToken(loginResponse.Content.AccessToken);
    }
}
