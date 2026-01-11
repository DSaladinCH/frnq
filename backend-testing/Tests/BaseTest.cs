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
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    }

    protected async Task AuthenticateAsync()
    {
        var loginResponse = await Api.Auth.Login(new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        });

        if (loginResponse?.Content?.AccessToken != null)
        {
            Api.SetToken(loginResponse.Content.AccessToken);
        }
    }
}
