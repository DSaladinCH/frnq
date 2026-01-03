using DSaladin.Frnq.Api.ModelBinders;
using DSaladin.Frnq.Api.Quote.Providers;
using DSaladin.Frnq.Api.Result;
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
	public async Task<ApiResponse> Search([FromQuery] string query, [FromQuery] string providerId = "yahoo-finance")
	{
		IFinanceProvider? financeProvider = registry.GetProvider(providerId);

		if (financeProvider is null)
			return ApiResponse.Create($"Provider with ID '{providerId}' not found.", System.Net.HttpStatusCode.BadRequest);

		IEnumerable<QuoteModel> results = await financeProvider.SearchAsync(query);

		// Remove duplicates based on Symbol, keeping the first occurrence
		results = results.GroupBy(q => q.Symbol).Select(g => g.First());
		return ApiResponse.Create(results, System.Net.HttpStatusCode.OK);
	}

	// GET: api/quote/historical?symbol=AAPL&from=2024-01-01&to=2024-01-10
	[HttpGet("historical")]
	public async Task<ApiResponse> GetHistoricalPrices([FromQuery] string symbol, [ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime from, [ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime to, [FromQuery] string providerId = "yahoo-finance")
	{
		return await quoteManagement.GetHistoricalPricesAsync(providerId, symbol, from, to);
	}

	[HttpPut("{quoteId}/customName")]
	public async Task<IActionResult> UpdateCustomName([FromRoute] int quoteId, [FromBody] CustomNameDto customName)
	{
		if (string.IsNullOrWhiteSpace(customName.CustomName))
			return BadRequest("Custom name cannot be empty.");

		return await quoteManagement.UpdateCustomNameAsync(quoteId, customName.CustomName);
	}

	[HttpDelete("{quoteId}/customName")]
	public async Task<ApiResponse> DeleteCustomName([FromRoute] int quoteId)
	{
		return await quoteManagement.DeleteCustomNameAsync(quoteId);
	}
}