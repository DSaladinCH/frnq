using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace DSaladin.Frnq.Api.Testing.Infrastructure;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

		Claim[] claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, DataSeeder.TestUserId.ToString()),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
		ClaimsIdentity identity = new ClaimsIdentity(claims, "Test");
		ClaimsPrincipal principal = new ClaimsPrincipal(identity);
		AuthenticationTicket ticket = new AuthenticationTicket(principal, "TestScheme");

		AuthenticateResult result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}
