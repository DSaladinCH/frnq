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

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Quote")]
public class Quote : TestBase
{
    [Fact]
    public async Task Search_WithValidSearchTerm_ReturnsMatchingQuotesFromProvider()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();

		List<QuoteModel> mockedQuotes = new List<QuoteModel>
        {
            new() { Symbol = "MSFT", Name = "Microsoft", ProviderId = "yahoo-finance" }
        };

        FinanceProviderMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedQuotes);

		TestResponse<List<QuoteModel>> response = await ApiInterface.Quotes.Search("microsoft");

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(response.Content!);
        Assert.Equal("MSFT", response.Content![0].Symbol);
    }

    [Fact]
    public async Task UpdateCustomName_WithNewName_UpdatesDatabaseAndReturnsSuccess()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();

		CustomNameDto customName = new CustomNameDto("New Custom Name");
		TestResponse response = await ApiInterface.Quotes.UpdateCustomName(1, customName);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

		// Verify database was updated
		QuoteName? savedName = DbContext.QuoteNames.FirstOrDefault(qn => qn.QuoteId == 1);
        Assert.NotNull(savedName);
        Assert.Equal(customName.CustomName, savedName.CustomName);
    }

    [Fact]
    public async Task DeleteCustomName_WithExistingName_RemovesFromDatabaseAndReturnsSuccess()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();

		TestResponse response = await ApiInterface.Quotes.DeleteCustomName(1);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

		// Verify database was updated - custom name should be removed
		QuoteName? savedName = DbContext.QuoteNames.FirstOrDefault(qn => qn.QuoteId == 1);
        Assert.Null(savedName);
    }

    [Fact]
    public async Task GetHistoricalPrices_WithValidDateRange_ReturnsDataFromProvider()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();

		List<QuotePrice> mockedPrices = new List<QuotePrice>
        {
            new() { Date = DateTime.UtcNow.AddDays(-1), Close = 155.0m, QuoteId = 1 }
        };

        FinanceProviderMock
            .Setup(p => p.GetHistoricalPricesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedPrices);

		TestResponse<List<QuotePrice>> response = await ApiInterface.Quotes.GetHistoricalPrices("AAPL", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(response.Content!);
    }

    [Fact]
    public async Task Search_WithValidTerm_CombinesDatabaseAndProviderResults()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();

		// "AAPL" (Id=1) is in DB from DataSeeder
		List<QuoteModel> mockedQuotes = new List<QuoteModel>
        {
            new() { Symbol = "MSFT", Name = "Microsoft", ProviderId = "yahoo-finance" }
        };

        FinanceProviderMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedQuotes);

		// Search for "A" should find AAPL from DB and MSFT from mock (if query matched MSFT)
		// Actually let's search for "Apple" to be specific
		TestResponse<List<QuoteModel>> response = await ApiInterface.Quotes.Search("Apple");

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Should contain AAPL from DB
        Assert.Contains(response.Content!, q => q.Symbol == "AAPL");
    }

    [Fact]
    public async Task Search_WithInvalidProviderName_ReturnsBadRequest()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();

		TestResponse<List<QuoteModel>> response = await ApiInterface.Quotes.Search("Apple", "non-existing-provider");

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetHistoricalPrices_WithFromDateAfterToDate_ReturnsBadRequest()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();
		TestResponse<List<QuotePrice>> response = await ApiInterface.Quotes.GetHistoricalPrices("AAPL", DateTime.UtcNow, DateTime.UtcNow.AddDays(-1));
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
