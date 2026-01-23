using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Auth OIDC")]
public class AuthOidc : TestBase
{
	[Fact]
	public async Task GetProviders()
	{
		ApiResponse<List<OidcProviderDto>> response = await ApiInterface.Oidc.GetProviders();

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		Assert.Contains(response.Value, p => p.ProviderId == "test-provider");
	}

	[Fact]
	public async Task InitiateLogin()
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
	public async Task CallbackInvalidState()
	{
		HttpClient client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false
		});

		HttpResponseMessage response = await client.GetAsync("api/AuthOidc/callback/test-provider?code=123&state=invalid");

		Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
		Assert.Contains("error=", response.Headers.Location!.ToString());
	}
}

[AllureSuite("Auth External Links")]
public class AuthExternalLinks : TestBase
{
	[Fact]
	public async Task GetLinks()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse<List<ExternalLinkViewDto>> response = await ApiInterface.ExternalLinks.GetLinks();

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		Assert.NotEmpty(response.Value);
		Assert.Contains(response.Value, l => l.ProviderId == "test-provider");
	}

	[Fact]
	public async Task InitiateLink()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse<object> response = await ApiInterface.ExternalLinks.InitiateLink("test-provider");

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact]
	public async Task UnlinkAccountValid()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		int externalUserLinkCount = await DbContext.ExternalUserLinks.CountAsync(e => e.UserId == DataSeeder.TestUserId);
		ExternalUserLink externalUserLink = await DbContext.ExternalUserLinks.FirstAsync(e => e.UserId == DataSeeder.TestUserId);

		ApiResponse response = await ApiInterface.ExternalLinks.UnlinkAccount(externalUserLink.Id);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		Assert.Equal(externalUserLinkCount - 1, await DbContext.ExternalUserLinks.CountAsync(e => e.UserId == DataSeeder.TestUserId));
	}

	[Fact]
	public async Task UnlinkAccountInvalid()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse response = await ApiInterface.ExternalLinks.UnlinkAccount(Guid.NewGuid());

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}
}
