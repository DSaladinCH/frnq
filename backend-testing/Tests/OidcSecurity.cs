using System.Net;
using System.Text;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DSaladin.Frnq.Api.Testing.Tests;

public class OidcSecurity : TestBase
{
	[Fact]
	public async Task CallbackRejectsUnsignedIdToken()
	{
		OidcProvider provider = new()
		{
			ProviderId = "signature-test",
			DisplayName = "Signature test",
			AuthorizationEndpoint = "https://issuer.example/authorize",
			TokenEndpoint = "https://issuer.example/token",
			ClientId = "client-id",
			ClientSecret = "client-secret"
		};
		DbContext.OidcProviders.Add(provider);
		await DbContext.SaveChangesAsync();
		DbContext.OidcStates.Add(new OidcState
		{
			State = "valid-state",
			ProviderId = provider.Id,
			Nonce = "expected-nonce",
			ExpiresAt = DateTime.UtcNow.AddMinutes(5)
		});
		await DbContext.SaveChangesAsync();

		IConfiguration configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["OidcProviders:signature-test:IssuerUrl"] = "https://issuer.example",
				["JwtSettings:SecretKey"] = "test-secret-that-is-long-enough-for-token-signing",
				["JwtSettings:Issuer"] = "frnq-api",
				["JwtSettings:Audience"] = "frnq-ui",
				["JwtSettings:AccessTokenExpiryInMinutes"] = "15"
			})
			.Build();
		IHttpContextAccessor accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
		AuthManagement authManagement = new(DbContext, configuration, accessor);
		OidcManagement oidcManagement = new(
			DbContext,
			configuration,
			accessor,
			authManagement,
			new TestHttpClientFactory(),
			NullLogger<OidcManagement>.Instance);

		ApiResponse<AuthResponseDto> response = await oidcManagement.HandleCallbackAsync("signature-test", "authorization-code", "valid-state", CancellationToken.None);

		Assert.True(response.Failed);
		Assert.Equal("USERINFO_FAILED", response.Code);
		Assert.True((await DbContext.OidcStates.SingleAsync()).IsUsed);
	}

	private sealed class TestHttpClientFactory : IHttpClientFactory
	{
		public HttpClient CreateClient(string name) => new(new TestHttpMessageHandler());
	}

	private sealed class TestHttpMessageHandler : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			string content = request.RequestUri!.AbsolutePath switch
			{
				"/token" => "{\"id_token\":\"unsigned-id-token\"}",
				"/.well-known/openid-configuration" => "{\"jwks_uri\":\"https://issuer.example/keys\"}",
				"/keys" => "{\"keys\":[]}",
				_ => "{}"
			};

			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(content, Encoding.UTF8, "application/json")
			});
		}
	}
}