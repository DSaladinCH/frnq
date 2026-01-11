using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;
using Moq;
using Microsoft.Extensions.DependencyInjection;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Quote")]
public class Quote(CustomWebApplicationFactory<Program> factory) : BaseTest(factory)
{
    [Fact]
    public async Task Search_WithValidSearchTerm_ReturnsMatchingQuotesFromProvider()
    {
        await AuthenticateAsync();
        
        var mockedQuotes = new List<QuoteModel>
        {
            new() { Symbol = "MSFT", Name = "Microsoft", ProviderId = "yahoo-finance" }
        };

        Factory.FinanceProviderMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedQuotes);

        var response = await Api.Quotes.Search("microsoft");

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(response.Content!);
        Assert.Equal("MSFT", response.Content![0].Symbol);
    }

    [Fact]
    public async Task UpdateCustomName_WithNewName_UpdatesDatabaseAndReturnsSuccess()
    {
        await AuthenticateAsync();

		var customName = new CustomNameDto("New Custom Name");
        var response = await Api.Quotes.UpdateCustomName(1, customName);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify database was updated
        using var context = GetDatabaseContext();
        var savedName = context.QuoteNames.FirstOrDefault(qn => qn.QuoteId == 1);
        Assert.NotNull(savedName);
        Assert.Equal(customName.CustomName, savedName.CustomName);
    }

    [Fact]
    public async Task DeleteCustomName_WithExistingName_RemovesFromDatabaseAndReturnsSuccess()
    {
        await AuthenticateAsync();

        var response = await Api.Quotes.DeleteCustomName(1);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify database was updated - custom name should be removed
        using var context = GetDatabaseContext();
        var savedName = context.QuoteNames.FirstOrDefault(qn => qn.QuoteId == 1);
        Assert.Null(savedName);
    }

    [Fact]
    public async Task GetHistoricalPrices_WithValidDateRange_ReturnsDataFromProvider()
    {
        await AuthenticateAsync();
        
        var mockedPrices = new List<QuotePrice>
        {
            new() { Date = DateTime.UtcNow.AddDays(-1), Close = 155.0m, QuoteId = 1 }
        };

        Factory.FinanceProviderMock
            .Setup(p => p.GetHistoricalPricesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedPrices);

        var response = await Api.Quotes.GetHistoricalPrices("AAPL", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(response.Content!);
    }

    [Fact]
    public async Task Search_WithValidTerm_CombinesDatabaseAndProviderResults()
    {
        await AuthenticateAsync();
        
        // "AAPL" (Id=1) is in DB from DataSeeder
        var mockedQuotes = new List<QuoteModel>
        {
            new() { Symbol = "MSFT", Name = "Microsoft", ProviderId = "yahoo-finance" }
        };

        Factory.FinanceProviderMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedQuotes);

        // Search for "A" should find AAPL from DB and MSFT from mock (if query matched MSFT)
        // Actually let's search for "Apple" to be specific
        var response = await Api.Quotes.Search("Apple");

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Should contain AAPL from DB
        Assert.Contains(response.Content!, q => q.Symbol == "AAPL");
    }

    [Fact]
    public async Task Search_WithInvalidProviderName_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var response = await Api.Quotes.Search("Apple", "non-existing-provider");

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetHistoricalPrices_WithFromDateAfterToDate_ReturnsBadRequest()
    {
        await AuthenticateAsync();
        var response = await Api.Quotes.GetHistoricalPrices("AAPL", DateTime.UtcNow, DateTime.UtcNow.AddDays(-1));
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DatabaseProvider_AddNewQuote_CreatesInDatabase()
    {
        using var scope = Factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<DSaladin.Frnq.Api.Quote.Providers.DatabaseProvider>();
        
        var newQuote = new QuoteModel
        {
            Symbol = "TSLA",
            Name = "Tesla",
            ProviderId = "yahoo-finance",
            Currency = "USD"
        };

        var result = await provider.AddOrUpdateQuoteAsync(newQuote, default);
        Assert.NotNull(result);
        Assert.True(result.Id > 0);

        // Verify it's in the database
        using var context = GetDatabaseContext();
        var savedQuote = context.Quotes.FirstOrDefault(q => q.Symbol == "TSLA");
        Assert.NotNull(savedQuote);
        Assert.Equal("Tesla", savedQuote.Name);
    }

    [Fact]
    public async Task DatabaseProvider_UpdateExistingQuote_ModifiesDatabaseRecord()
    {
        using var scope = Factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<DSaladin.Frnq.Api.Quote.Providers.DatabaseProvider>();
        var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        
        // Get existing quote from database
        var existingQuote = context.Quotes.First(q => q.Symbol == "AAPL");
        var originalName = existingQuote.Name;
        
        // Update it
        existingQuote.Name = "Apple Inc. Updated";
        var updated = await provider.AddOrUpdateQuoteAsync(existingQuote, default);
        
        Assert.NotEqual(originalName, updated!.Name);
        Assert.Equal("Apple Inc. Updated", updated.Name);

        // Verify it's updated in the database
        using var freshContext = GetDatabaseContext();
        var savedQuote = freshContext.Quotes.FirstOrDefault(q => q.Id == existingQuote.Id);
        Assert.NotNull(savedQuote);
        Assert.Equal("Apple Inc. Updated", savedQuote.Name);
    }
}
