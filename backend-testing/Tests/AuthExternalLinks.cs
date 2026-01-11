using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Auth OIDC")]
public class AuthOidc(CustomWebApplicationFactory<Program> factory) : BaseTest(factory)
{
    [Fact]
    public async Task GetProviders_ReturnsListWithEnabledProviders()
    {
        var response = await Api.Oidc.GetProviders();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        // We seeded 'test-provider' as enabled
        Assert.Contains(response.Content, p => p.ProviderId == "test-provider");
    }

    [Fact]
    public async Task InitiateLogin_ReturnsRedirect()
    {
        var client = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("api/AuthOidc/login/test-provider");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("example.com/auth", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Callback_WithInvalidState_RedirectsToLoginWithError()
    {
        var client = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Current OidcManagement redirects on failure
        var response = await client.GetAsync("api/AuthOidc/callback/test-provider?code=123&state=invalid");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("error=", response.Headers.Location!.ToString());
    }
}

[AllureSuite("Auth External Links")]
public class AuthExternalLinks(CustomWebApplicationFactory<Program> factory) : BaseTest(factory)
{
    [Fact]
    public async Task GetLinks_WhenAuthenticated_ReturnsListWithSeededLink()
    {
        await AuthenticateAsync();
        var response = await Api.ExternalLinks.GetLinks();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.NotEmpty(response.Content);
        Assert.Contains(response.Content, l => l.ProviderId == "test-provider");
    }

    [Fact]
    public async Task InitiateLink_WhenAuthenticated_ReturnsAuthUrl()
    {
        await AuthenticateAsync();
        var response = await Api.ExternalLinks.InitiateLink("test-provider");

        Assert.NotNull(response);
        // It should return 200 OK with the authorization URL
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnlinkAccount_WithValidId_ReturnsOk()
    {
        await AuthenticateAsync();
        // The seeded link ID is 00000000-0000-0000-0000-000000000003
        var linkId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var response = await Api.ExternalLinks.UnlinkAccount(linkId);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnlinkAccount_WithNonExistentId_ReturnsNotFound()
    {
        await AuthenticateAsync();
        var response = await Api.ExternalLinks.UnlinkAccount(Guid.NewGuid());

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
