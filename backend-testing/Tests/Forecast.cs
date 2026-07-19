using System.Net;
using Allure.Xunit.Attributes;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Forecast;
using DSaladin.Frnq.Api.Group;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Result;
using Xunit;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Forecast")]
public class Forecast : TestBase
{
	private const int TradingDaysPerYear = 252;

	private static UserModel NewUser(string name) => new()
	{
		Email = $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com",
		Firstname = name,
		PasswordHash = "irrelevant"
	};

	private static QuoteModel NewQuote(string symbol) => new()
	{
		ProviderId = "yahoo-finance",
		Symbol = symbol,
		Name = symbol,
		Currency = "CHF",
		ExchangeDisposition = "NasdaqGS",
		TypeDisposition = "EQUITY"
	};

	// Seeds `days` consecutive daily prices ending the day before `endDateExclusive`, all at the
	// same price. A flat price series means every simulated daily ratio is exactly 1.0, so the
	// Monte Carlo simulation's random path selection can no longer affect the result - this is
	// what makes the "success" tests below deterministic instead of depending on randomness.
	private static void SeedFlatPrices(DatabaseContext db, QuoteModel quote, int days, decimal price, DateTime endDateExclusive)
	{
		for (int i = days; i >= 1; i--)
		{
			db.QuotePrices.Add(new QuotePrice
			{
				QuoteId = quote.Id,
				Date = endDateExclusive.AddDays(-i),
				Close = price,
				AdjustedClose = price
			});
		}
	}

	private static InvestmentModel Buy(UserModel user, QuoteModel quote, DateTime date, decimal amount, decimal pricePerUnit) => new()
	{
		UserId = user.Id,
		QuoteId = quote.Id,
		Date = date,
		Amount = amount,
		PricePerUnit = pricePerUnit,
		Type = InvestmentType.Buy
	};

	[Fact]
	public async Task GetForecastUnauthenticated()
	{
		ApiResponse<List<ForecastDayDto>> response = await ApiInterface.Forecast.GetForecast();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Null(response.Value);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(31)]
	public async Task GetForecastInvalidYears(int years)
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse<List<ForecastDayDto>> response = await ApiInterface.Forecast.GetForecast(years);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("INVALID PARAMETER", response.Code);
	}

	[Fact]
	public async Task GetForecastNoActivePositions()
	{
		UserModel user = NewUser("NoPositions");
		DbContext.Users.Add(user);
		await DbContext.SaveChangesAsync();

		using AuthenticationScope<UserModel> authScope = Authenticate(user);

		ApiResponse<List<ForecastDayDto>> response = await ApiInterface.Forecast.GetForecast();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		Assert.Empty(response.Value);
	}

	[Fact]
	public async Task GetForecastInvalidPriceData()
	{
		UserModel user = NewUser("InvalidPrice");
		QuoteModel quote = NewQuote("BAD");
		DbContext.Users.Add(user);
		DbContext.Quotes.Add(quote);
		await DbContext.SaveChangesAsync();

		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = DateTime.UtcNow.Date,
			Close = 100m,
			AdjustedClose = 0m // invalid: zero adjusted close
		});
		DbContext.Investments.Add(Buy(user, quote, DateTime.UtcNow.Date.AddDays(-30), 10m, 100m));
		await DbContext.SaveChangesAsync();

		using AuthenticationScope<UserModel> authScope = Authenticate(user);

		ApiResponse<List<ForecastDayDto>> response = await ApiInterface.Forecast.GetForecast();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("INVALID DATA", response.Code);
	}

	[Fact]
	public async Task GetForecastNoOverlappingHistory()
	{
		UserModel user = NewUser("NoOverlap");
		QuoteModel quoteA = NewQuote("ODD");
		QuoteModel quoteB = NewQuote("EVN");
		DbContext.Users.Add(user);
		DbContext.Quotes.AddRange(quoteA, quoteB);
		await DbContext.SaveChangesAsync();

		DateTime baseDate = DateTime.UtcNow.Date;
		// quoteA is only priced on odd day-offsets, quoteB only on even ones: no shared date exists.
		for (int i = 1; i <= 5; i += 2)
			DbContext.QuotePrices.Add(new QuotePrice { QuoteId = quoteA.Id, Date = baseDate.AddDays(-i), Close = 100m, AdjustedClose = 100m });
		for (int i = 2; i <= 6; i += 2)
			DbContext.QuotePrices.Add(new QuotePrice { QuoteId = quoteB.Id, Date = baseDate.AddDays(-i), Close = 50m, AdjustedClose = 50m });

		DbContext.Investments.Add(Buy(user, quoteA, baseDate.AddDays(-30), 10m, 100m));
		DbContext.Investments.Add(Buy(user, quoteB, baseDate.AddDays(-30), 10m, 50m));
		await DbContext.SaveChangesAsync();

		using AuthenticationScope<UserModel> authScope = Authenticate(user);

		ApiResponse<List<ForecastDayDto>> response = await ApiInterface.Forecast.GetForecast();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("INSUFFICIENT DATA", response.Code);
		Assert.Contains("Not enough overlapping", response.Description);
	}

	[Fact]
	public async Task GetForecastInsufficientReturnHistory()
	{
		UserModel user = NewUser("ShortHistory");
		QuoteModel quote = NewQuote("SHRT");
		DbContext.Users.Add(user);
		DbContext.Quotes.Add(quote);
		await DbContext.SaveChangesAsync();

		DateTime baseDate = DateTime.UtcNow.Date;
		SeedFlatPrices(DbContext, quote, days: 5, price: 100m, endDateExclusive: baseDate); // 5 overlapping days -> 4 returns < blockLength (10)
		DbContext.Investments.Add(Buy(user, quote, baseDate.AddDays(-30), 10m, 100m));
		await DbContext.SaveChangesAsync();

		using AuthenticationScope<UserModel> authScope = Authenticate(user);

		ApiResponse<List<ForecastDayDto>> response = await ApiInterface.Forecast.GetForecast();

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("INSUFFICIENT DATA", response.Code);
		Assert.Contains("4 days", response.Description);
	}

	[Fact]
	public async Task GetForecastFlatPricesNoContributionsIsDeterministic()
	{
		UserModel user = NewUser("Flat");
		QuoteModel quoteA = NewQuote("FLTA"); // grouped
		QuoteModel quoteB = NewQuote("FLTB"); // grouped, same group as A
		QuoteModel quoteC = NewQuote("FLTC"); // ungrouped
		DbContext.Users.Add(user);
		DbContext.Quotes.AddRange(quoteA, quoteB, quoteC);
		await DbContext.SaveChangesAsync();

		QuoteGroup group = new() { UserId = user.Id, Name = "Group1" };
		DbContext.QuoteGroups.Add(group);
		await DbContext.SaveChangesAsync();

		DbContext.QuoteGroupMappings.Add(new QuoteGroupMapping { UserId = user.Id, QuoteId = quoteA.Id, GroupId = group.Id });
		DbContext.QuoteGroupMappings.Add(new QuoteGroupMapping { UserId = user.Id, QuoteId = quoteB.Id, GroupId = group.Id });

		DateTime baseDate = DateTime.UtcNow.Date;
		const decimal priceA = 100m, priceB = 200m, priceC = 50m;
		SeedFlatPrices(DbContext, quoteA, days: 15, price: priceA, endDateExclusive: baseDate);
		SeedFlatPrices(DbContext, quoteB, days: 15, price: priceB, endDateExclusive: baseDate);
		SeedFlatPrices(DbContext, quoteC, days: 15, price: priceC, endDateExclusive: baseDate);

		const decimal unitsA = 10m, unitsB = 5m, unitsC = 8m;
		DbContext.Investments.Add(Buy(user, quoteA, baseDate.AddDays(-30), unitsA, priceA));
		DbContext.Investments.Add(Buy(user, quoteB, baseDate.AddDays(-30), unitsB, priceB));
		DbContext.Investments.Add(Buy(user, quoteC, baseDate.AddDays(-30), unitsC, priceC));
		await DbContext.SaveChangesAsync();

		double expectedValueA = (double)(unitsA * priceA); // 1000
		double expectedValueB = (double)(unitsB * priceB); // 1000
		double expectedValueC = (double)(unitsC * priceC); // 400
		double expectedGroupSum = expectedValueA + expectedValueB; // 2000
		double expectedPortfolio = expectedGroupSum + expectedValueC; // 2400

		using AuthenticationScope<UserModel> authScope = Authenticate(user);

		ApiResponse<List<ForecastDayDto>> response = await ApiInterface.Forecast.GetForecast(years: 1, includeContributions: false);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		Assert.Equal(TradingDaysPerYear, response.Value.Count);

		DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
		DateOnly previousDate = today;
		foreach (ForecastDayDto day in response.Value)
		{
			Assert.True(day.Date > previousDate);
			Assert.NotEqual(DayOfWeek.Saturday, day.Date.DayOfWeek);
			Assert.NotEqual(DayOfWeek.Sunday, day.Date.DayOfWeek);
			previousDate = day.Date;
		}

		foreach (int dayIndex in new[] { 0, response.Value.Count / 2, response.Value.Count - 1 })
		{
			ForecastDayDto day = response.Value[dayIndex];

			Assert.Equal(expectedPortfolio, day.Portfolio.Median, precision: 6);
			Assert.Equal(expectedPortfolio, day.Portfolio.Lower, precision: 6);
			Assert.Equal(expectedPortfolio, day.Portfolio.Upper, precision: 6);

			ForecastQuoteDto quoteAForecast = day.Quotes.Single(q => q.QuoteId == quoteA.Id);
			ForecastQuoteDto quoteBForecast = day.Quotes.Single(q => q.QuoteId == quoteB.Id);
			ForecastQuoteDto quoteCForecast = day.Quotes.Single(q => q.QuoteId == quoteC.Id);
			Assert.Equal(expectedValueA, quoteAForecast.Band.Median, precision: 6);
			Assert.Equal(expectedValueB, quoteBForecast.Band.Median, precision: 6);
			Assert.Equal(expectedValueC, quoteCForecast.Band.Median, precision: 6);

			Assert.Equal(2, day.Groups.Count); // the real group + the "ungrouped" (null) pseudo-group
			ForecastGroupDto realGroup = day.Groups.Single(g => g.GroupId == group.Id);
			ForecastGroupDto ungroupedGroup = day.Groups.Single(g => g.GroupId == null);
			Assert.Equal(expectedGroupSum, realGroup.Band.Median, precision: 6);
			Assert.Equal(expectedValueC, ungroupedGroup.Band.Median, precision: 6);
		}
	}

	[Fact]
	public async Task GetForecastContributionsDisabledStaysFlatDespiteRecurringBuys()
	{
		UserModel user = NewUser("ContribOff");
		QuoteModel quote = NewQuote("COFF");
		DbContext.Users.Add(user);
		DbContext.Quotes.Add(quote);
		await DbContext.SaveChangesAsync();

		DateTime baseDate = DateTime.UtcNow.Date;
		const decimal price = 100m;
		SeedFlatPrices(DbContext, quote, days: 15, price: price, endDateExclusive: baseDate);

		const decimal units = 10m;
		DbContext.Investments.Add(Buy(user, quote, baseDate.AddDays(-90), units, price));
		DbContext.Investments.Add(Buy(user, quote, baseDate.AddDays(-60), units, price));
		DbContext.Investments.Add(Buy(user, quote, baseDate.AddDays(-30), units, price));
		await DbContext.SaveChangesAsync();

		double expectedValue = (double)(units * 3 * price); // 3000, unchanged over the whole horizon

		using AuthenticationScope<UserModel> authScope = Authenticate(user);

		ApiResponse<List<ForecastDayDto>> response = await ApiInterface.Forecast.GetForecast(years: 1, includeContributions: false);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		Assert.Equal(expectedValue, response.Value[0].Portfolio.Median, precision: 6);
		Assert.Equal(expectedValue, response.Value[^1].Portfolio.Median, precision: 6);
	}

	[Fact]
	public async Task GetForecastWithContributionsGrowsInDiscreteSteps()
	{
		UserModel user = NewUser("ContribOn");
		QuoteModel quote = NewQuote("CON");
		DbContext.Users.Add(user);
		DbContext.Quotes.Add(quote);
		await DbContext.SaveChangesAsync();

		DateTime baseDate = DateTime.UtcNow.Date;
		const decimal price = 100m;
		SeedFlatPrices(DbContext, quote, days: 15, price: price, endDateExclusive: baseDate);

		// Three buys spaced exactly 30 calendar days apart -> a regular contribution cadence is derivable.
		const decimal units = 10m;
		DbContext.Investments.Add(Buy(user, quote, baseDate.AddDays(-90), units, price));
		DbContext.Investments.Add(Buy(user, quote, baseDate.AddDays(-60), units, price));
		DbContext.Investments.Add(Buy(user, quote, baseDate.AddDays(-30), units, price));
		await DbContext.SaveChangesAsync();

		double initialValue = (double)(units * 3 * price); // 3000
		double expectedContribution = (double)(units * price); // 1000 (avg cash per purchase)

		using AuthenticationScope<UserModel> authScope = Authenticate(user);

		ApiResponse<List<ForecastDayDto>> response = await ApiInterface.Forecast.GetForecast(years: 1, includeContributions: true);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);

		double previous = initialValue;
		bool sawGrowth = false;
		foreach (ForecastDayDto day in response.Value)
		{
			double current = day.Portfolio.Median;

			// Ratio is always exactly 1.0 (flat prices), so value can only stay flat or jump by
			// exactly one contribution - it can never decrease and can never move a partial amount.
			Assert.True(current >= previous - 1e-6);
			double delta = current - previous;
			if (delta > 1e-6)
			{
				Assert.Equal(expectedContribution, delta, precision: 6);
				sawGrowth = true;
			}

			// Single quote, no group: portfolio, group and quote series are all the same number.
			Assert.Equal(current, day.Quotes.Single().Band.Median, precision: 6);

			previous = current;
		}

		Assert.True(sawGrowth, "Expected at least one contribution to be applied within the 1-year horizon.");
		Assert.True(response.Value[^1].Portfolio.Median > initialValue);
	}
}
