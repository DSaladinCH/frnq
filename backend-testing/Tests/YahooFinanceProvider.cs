using Allure.Xunit.Attributes;
using DSaladin.Frnq.Api.Quote;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Provider")]
public class YahooFinanceProvider: TestBase
{
    private readonly Frnq.Api.Quote.Providers.YahooFinanceProvider _provider = new();

    [Fact]
    public async Task GetQuoteAsync_WithValidSymbol_ReturnsQuote()
    {
		// This test might fail if there is no internet connection, 
		// but it verifies the implementation against the real external API as requested.
		QuoteModel? quote = await _provider.GetQuoteAsync("AAPL", CancellationToken.None);

        Assert.NotNull(quote);
        Assert.Equal("AAPL", quote.Symbol);
        Assert.NotEmpty(quote.Name);
    }

    [Fact]
    public async Task SearchAsync_WithQuery_ReturnsResults()
    {
		IEnumerable<QuoteModel> results = await _provider.SearchAsync("Apple", CancellationToken.None);

        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.Contains(results, q => q.Symbol == "AAPL");
    }

    [Fact]
    public async Task GetQuoteAsync_WithInvalidSymbol_ReturnsNull()
    {
		QuoteModel? quote = await _provider.GetQuoteAsync("NONEXISTENT_SYMBOL_12345", CancellationToken.None);
        Assert.Null(quote);
    }
}
