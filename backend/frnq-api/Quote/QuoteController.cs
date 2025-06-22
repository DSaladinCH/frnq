using DSaladin.Frnq.Api.ModelBinders;
using DSaladin.Frnq.Api.Quote.Providers;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Quote;

[ApiController]
[Route("api/[controller]")]
public class QuoteController(QuoteManagement quoteManagement, ProviderRegistry registry) : ControllerBase
{
    // GET: api/quote/search?query=apple&providerId=yahoo-finance
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] string providerId = "yahoo-finance")
    {
        IFinanceProvider? financeProvider = registry.GetProvider(providerId);

        if (financeProvider is null)
            return BadRequest($"Provider with ID '{providerId}' not found.");

        return Ok(await financeProvider.SearchAsync(query));
    }

    // GET: api/quote/historical?symbol=AAPL&from=2024-01-01&to=2024-01-10
    [HttpGet("historical")]
    public async Task<IActionResult> GetHistoricalPrices([FromQuery] string symbol, [ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime from, [ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime to, [FromQuery] string providerId = "yahoo-finance")
    {
        return Ok(await quoteManagement.GetHistoricalPricesAsync(providerId, symbol, from, to));
    }
}