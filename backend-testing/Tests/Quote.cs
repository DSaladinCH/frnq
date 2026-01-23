using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Quote.Providers;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Quote")]
public class Quote : TestBase
{
	[Fact]
	public async Task SearchValid()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		List<QuoteModel> mockedQuotes =
		[
			new() { Symbol = "MSFT", Name = "Microsoft", ProviderId = "yahoo-finance" }
		];

		FinanceProviderMock
			.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockedQuotes);

		ApiResponse<List<QuoteModel>> response = await ApiInterface.Quotes.Search("microsoft");

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Single(response.Value!);
		Assert.Equal("MSFT", response.Value![0].Symbol);
	}

	[Fact]
	public async Task SearchCombinesDatabaseAndProvider()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		List<QuoteModel> mockedQuotes =
		[
			new() { Symbol = "MSFT", Name = "Microsoft", ProviderId = "yahoo-finance" }
		];

		FinanceProviderMock
			.Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockedQuotes);

		ApiResponse<List<QuoteModel>> response = await ApiInterface.Quotes.Search("Apple");

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains(response.Value!, q => q.Symbol == "AAPL");
	}

	[Fact]
	public async Task SearchInvalidProvider()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse<List<QuoteModel>> response = await ApiInterface.Quotes.Search("Apple", "non-existing-provider");

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task UpdateCustomNameValid()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		CustomNameDto customName = new("New Custom Name");
		ApiResponse response = await ApiInterface.Quotes.UpdateCustomName(1, customName);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

		DbContext.ChangeTracker.Clear();

		QuoteName? savedName = await DbContext.QuoteNames.FirstOrDefaultAsync(qn => qn.QuoteId == 1);
		Assert.NotNull(savedName);
		Assert.Equal(customName.CustomName, savedName.CustomName);
	}

	[Fact]
	public async Task DeleteCustomNameValid()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		int quoteNameCountBefore = await DbContext.QuoteNames.CountAsync(qn => qn.UserId == DataSeeder.TestUserId);
		QuoteName quoteName = await DbContext.QuoteNames.FirstAsync(qn => qn.UserId == DataSeeder.TestUserId);

		ApiResponse response = await ApiInterface.Quotes.DeleteCustomName(quoteName.QuoteId);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

		DbContext.ChangeTracker.Clear();

		Assert.Equal(quoteNameCountBefore - 1, await DbContext.QuoteNames.CountAsync(qn => qn.UserId == DataSeeder.TestUserId));
	}

	[Fact]
	public async Task GetHistoricalPricesValid()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		List<QuotePrice> mockedPrices =
		[
			new() { Date = DateTime.UtcNow.AddDays(-1), Close = 155.0m, QuoteId = 1 }
		];

		FinanceProviderMock
			.Setup(p => p.GetHistoricalPricesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockedPrices);

		ApiResponse<List<QuotePrice>> response = await ApiInterface.Quotes.GetHistoricalPrices("AAPL", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotEmpty(response.Value!);
	}

	[Fact]
	public async Task GetHistoricalPricesInvalidDateRange()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse<List<QuotePrice>> response = await ApiInterface.Quotes.GetHistoricalPrices("AAPL", DateTime.UtcNow, DateTime.UtcNow.AddDays(-1));

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}
}
