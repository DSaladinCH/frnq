using DSaladin.Frnq.Api.ModelBinders;
using DSaladin.Frnq.Api.Quote.Providers;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Quote;

[ApiController]
[Route("api/[controller]")]
public class QuoteController(QuoteManagement quoteManagement, IFinanceProvider financeProvider) : ControllerBase
{
    // GET: api/quote/search?query=apple
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        return Ok(await financeProvider.SearchAsync(query));
    }

    // GET: api/quote/historical?symbol=AAPL&from=2024-01-01&to=2024-01-10
    [HttpGet("historical")]
    public async Task<IActionResult> GetHistoricalPrices([FromQuery] string symbol, [ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime from, [ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime to)
    {
        return Ok(await quoteManagement.GetHistoricalPricesAsync(financeProvider.InternalId, symbol, from, to));
    }
}