using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;
using DSaladin.Frnq.Api.Testing.Api;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Auth OIDC")]
public class AuthOidc : TestBase
{
    [Fact]
    public async Task GetProviders_ReturnsListWithEnabledProviders()
    {
		TestResponse<List<OidcProviderDto>> response = await ApiInterface.Oidc.GetProviders();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        // We seeded 'test-provider' as enabled
        Assert.Contains(response.Content, p => p.ProviderId == "test-provider");
    }

    [Fact]
    public async Task InitiateLogin_ReturnsRedirect()
    {
		HttpClient client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

		HttpResponseMessage response = await client.GetAsync("api/AuthOidc/login/test-provider");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("example.com/auth", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Callback_WithInvalidState_RedirectsToLoginWithError()
    {
		HttpClient client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

		// Current OidcManagement redirects on failure
		HttpResponseMessage response = await client.GetAsync("api/AuthOidc/callback/test-provider?code=123&state=invalid");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("error=", response.Headers.Location!.ToString());
    }
}

[AllureSuite("Auth External Links")]
public class AuthExternalLinks : TestBase
{
    [Fact]
    public async Task GetLinks_WhenAuthenticated_ReturnsListWithSeededLink()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();
		TestResponse<List<ExternalLinkViewDto>> response = await ApiInterface.ExternalLinks.GetLinks();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.NotEmpty(response.Content);
        Assert.Contains(response.Content, l => l.ProviderId == "test-provider");
    }

    [Fact]
    public async Task InitiateLink_WhenAuthenticated_ReturnsAuthUrl()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();
		TestResponse<object> response = await ApiInterface.ExternalLinks.InitiateLink("test-provider");

        Assert.NotNull(response);
        // It should return 200 OK with the authorization URL
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnlinkAccount_WithValidId_ReturnsOk()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();
		// The seeded link ID is 00000000-0000-0000-0000-000000000003
		Guid linkId = Guid.Parse("00000000-0000-0000-0000-000000000003");
		TestResponse response = await ApiInterface.ExternalLinks.UnlinkAccount(linkId);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnlinkAccount_WithNonExistentId_ReturnsNotFound()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();
		TestResponse response = await ApiInterface.ExternalLinks.UnlinkAccount(Guid.NewGuid());

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
