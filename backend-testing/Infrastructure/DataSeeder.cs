using DSaladin.Frnq.Api;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Group;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Testing.Infrastructure;

public static class DataSeeder
{
	public static Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
	public static string TestUserEmail = "test@example.com";
	public static string TestUserPassword = "Password123!";

	public static void Seed(DatabaseContext context)
	{
		if (context.Users.Any()) return;

		UserModel user = new UserModel
		{
			Id = TestUserId,
			Firstname = "Test",
			Email = TestUserEmail,
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(TestUserPassword)
		};

		context.Users.Add(user);

		QuoteGroup group = new QuoteGroup
		{
			Id = 1,
			UserId = user.Id,
			Name = "Test Group"
		};
		context.QuoteGroups.Add(group);

		QuoteModel quote = new QuoteModel
		{
			Id = 1,
			ProviderId = "yahoo-finance",
			Symbol = "AAPL",
			Name = "Apple Inc.",
			Currency = "USD",
			ExchangeDisposition = "NasdaqGS",
			TypeDisposition = "EQUITY"
		};
		context.Quotes.Add(quote);

		QuoteName quoteName = new QuoteName
		{
			QuoteId = quote.Id,
			UserId = user.Id,
			CustomName = "My Apple Stock"
		};
		context.QuoteNames.Add(quoteName);

		QuotePrice price = new QuotePrice
		{
			Id = 1,
			QuoteId = 1,
			Date = DateTime.UtcNow.Date.AddDays(-1),
			Close = 150.0m
		};
		context.QuotePrices.Add(price);

		InvestmentModel investment = new InvestmentModel
		{
			Id = 1,
			UserId = user.Id,
			QuoteId = 1,
			Date = DateTime.UtcNow.Date.AddDays(-1),
			Amount = 10,
			PricePerUnit = 145.0m,
			TotalFees = 1.0m,
			Type = InvestmentType.Buy
		};
		context.Investments.Add(investment);

		OidcProvider oidcProvider = new OidcProvider
		{
			Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
			ProviderId = "test-provider",
			DisplayName = "Test Provider",
			IsEnabled = true,
			AuthorizationEndpoint = "https://example.com/auth",
			TokenEndpoint = "https://example.com/token",
			ClientId = "test-client",
			ClientSecret = "test-secret"
		};
		context.OidcProviders.Add(oidcProvider);

		ExternalUserLink externalLink = new ExternalUserLink
		{
			Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
			UserId = user.Id,
			ProviderId = oidcProvider.Id,
			ProviderUserId = "external-user-id",
			CreatedAt = DateTime.UtcNow
		};
		context.ExternalUserLinks.Add(externalLink);

		context.SaveChanges();
	}

	public static async Task<UserModel> GetTestUser(DatabaseContext dbContext)
	{
		UserModel? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == TestUserEmail)
			?? throw new InvalidOperationException("Test user not found in the database.");

		return user;
	}
}
