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

	public static Guid TestUser2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
	public static string TestUser2Email = "test2@example.com";
	public static string TestUser2Password = "123!Password";

	public static async Task Seed(DatabaseContext context)
	{
		if (context.Users.Any()) return;

		await AddSetupData(context);
		await AddUser(TestUserId, "Test", TestUserEmail, TestUserPassword, context);
		await AddUser(TestUser2Id, "Test2", TestUser2Email, TestUser2Password, context);

		context.SaveChanges();
	}

	private static async Task AddSetupData(DatabaseContext context)
	{
		OidcProvider oidcProvider = new()
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

		await context.OidcProviders.AddAsync(oidcProvider);

		QuoteModel quote = new()
		{
			Id = 1,
			ProviderId = "yahoo-finance",
			Symbol = "AAPL",
			Name = "Apple Inc.",
			Currency = "USD",
			ExchangeDisposition = "NasdaqGS",
			TypeDisposition = "EQUITY"
		};
		await context.Quotes.AddAsync(quote);

		await AddPrice(quote, DateTime.UtcNow.AddDays(-3), 150.0m, context);
		await AddPrice(quote, DateTime.UtcNow.AddDays(-2), 152.0m, context);
		await AddPrice(quote, DateTime.UtcNow.AddDays(-1), 154.0m, context);

		QuoteModel quote2 = new()
		{
			Id = 2,
			ProviderId = "yahoo-finance",
			Symbol = "GOOGL",
			Name = "Alphabet Inc.",
			Currency = "USD",
			ExchangeDisposition = "NasdaqGS",
			TypeDisposition = "EQUITY"
		};
		await context.Quotes.AddAsync(quote2);

		await AddPrice(quote2, DateTime.UtcNow.AddDays(-3), 2800.0m, context);
		await AddPrice(quote2, DateTime.UtcNow.AddDays(-2), 2825.0m, context);
		await AddPrice(quote2, DateTime.UtcNow.AddDays(-1), 2850.0m, context);

		await context.SaveChangesAsync();
	}

	private static async Task AddPrice(QuoteModel quote, DateTime date, decimal close, DatabaseContext context)
	{
		QuotePrice price = new()
		{
			QuoteId = quote.Id,
			Date = date,
			Close = close
		};

		await context.QuotePrices.AddAsync(price);
	}

	private static async Task AddUser(Guid id, string firstname, string email, string password, DatabaseContext context)
	{
		UserModel user = new()
		{
			Id = id,
			Firstname = firstname,
			Email = email,
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
		};

		await context.Users.AddAsync(user);

		ExternalUserLink externalLink = new()
		{
			UserId = user.Id,
			ProviderId = context.OidcProviders.First().Id,
			ProviderUserId = "external-user-id",
			CreatedAt = DateTime.UtcNow
		};
		await context.ExternalUserLinks.AddAsync(externalLink);

		QuoteGroup group = new()
		{
			UserId = user.Id,
			Name = $"{user.Firstname} - Test Group"
		};
		await context.QuoteGroups.AddAsync(group);

		foreach (QuoteModel quote in context.Quotes)
		{
			QuoteName quoteName = new()
			{
				QuoteId = quote.Id,
				UserId = user.Id,
				CustomName = $"{user.Firstname} - My {quote.Symbol}"
			};
			await context.QuoteNames.AddAsync(quoteName);

			QuoteGroupMapping groupMapping = new()
			{
				UserId = user.Id,
				QuoteId = quote.Id,
				GroupId = group.Id
			};
			await context.QuoteGroupMappings.AddAsync(groupMapping);

			for (int i = 0; i < 10; i++)
			{
				await CreateInvestment(
					user,
					quote,
					DateTime.UtcNow.Date.AddDays(-20 + i),
					amount: 20 + i,
					pricePerUnit: 150.0m + (i * 2),
					totalFees: 1.0m + (i * 0.1m),
					type: InvestmentType.Buy,
					context);
			}
		}

		await context.SaveChangesAsync();
	}

	private static async Task CreateInvestment(UserModel user, QuoteModel quote, DateTime date, decimal amount, decimal pricePerUnit, decimal totalFees, InvestmentType type, DatabaseContext context)
	{
		InvestmentModel investment = new()
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = date,
			Amount = amount,
			PricePerUnit = pricePerUnit,
			TotalFees = totalFees,
			Type = type
		};

		await context.Investments.AddAsync(investment);
	}

	public static async Task<UserModel> GetTestUser(DatabaseContext dbContext)
	{
		UserModel? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == TestUserEmail)
			?? throw new InvalidOperationException("Test user not found in the database.");

		return user;
	}

	public static async Task<UserModel> GetTestUser2(DatabaseContext dbContext)
	{
		UserModel? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == TestUser2Email)
			?? throw new InvalidOperationException("Test user not found in the database.");

		return user;
	}
}
