using Allure.Xunit.Attributes;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Provider")]
public class YahooFinanceProvider
{
    private readonly Frnq.Api.Quote.Providers.YahooFinanceProvider _provider = new();

    [Fact]
    public async Task GetQuoteAsync_WithValidSymbol_ReturnsQuote()
    {
        // This test might fail if there is no internet connection, 
        // but it verifies the implementation against the real external API as requested.
        var quote = await _provider.GetQuoteAsync("AAPL");

        Assert.NotNull(quote);
        Assert.Equal("AAPL", quote.Symbol);
        Assert.NotEmpty(quote.Name);
    }

    [Fact]
    public async Task SearchAsync_WithQuery_ReturnsResults()
    {
        var results = await _provider.SearchAsync("Apple");

        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.Contains(results, q => q.Symbol == "AAPL");
    }

    [Fact]
    public async Task GetQuoteAsync_WithInvalidSymbol_ReturnsNull()
    {
        var quote = await _provider.GetQuoteAsync("NONEXISTENT_SYMBOL_12345");
        Assert.Null(quote);
    }
}
