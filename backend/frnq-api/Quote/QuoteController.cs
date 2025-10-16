using DSaladin.Frnq.Api.ModelBinders;
using DSaladin.Frnq.Api.Quote.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Quote;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuoteController(QuoteManagement quoteManagement, ProviderRegistry registry) : ControllerBase
{
    // GET: api/quote/search?query=apple&providerId=yahoo-finance
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] string providerId = "yahoo-finance")
    {
        IFinanceProvider? financeProvider = registry.GetProvider(providerId);

        if (financeProvider is null)
            return BadRequest($"Provider with ID '{providerId}' not found.");

        IEnumerable<QuoteModel> results = await financeProvider.SearchAsync(query);

        // Remove duplicates based on Symbol, keeping the first occurrence
        results = results.GroupBy(q => q.Symbol).Select(g => g.First());
        return Ok(results);
    }

    // GET: api/quote/historical?symbol=AAPL&from=2024-01-01&to=2024-01-10
    [HttpGet("historical")]
    public async Task<IActionResult> GetHistoricalPrices([FromQuery] string symbol, [ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime from, [ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime to, [FromQuery] string providerId = "yahoo-finance")
    {
        return Ok(await quoteManagement.GetHistoricalPricesAsync(providerId, symbol, from, to));
    }

    [HttpPut("{quoteId}/customName")]
    public async Task<IActionResult> UpdateCustomName([FromRoute] int quoteId, [FromBody] CustomNameDto customName)
    {
        if (string.IsNullOrWhiteSpace(customName.CustomName))
            return BadRequest("Custom name cannot be empty.");

        bool result = await quoteManagement.UpdateCustomNameAsync(quoteId, customName.CustomName);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{quoteId}/customName")]
    public async Task<IActionResult> DeleteCustomName([FromRoute] int quoteId)
    {
        bool result = await quoteManagement.DeleteCustomNameAsync(quoteId);
        if (!result)
            return NotFound();

        return NoContent();
    }
}