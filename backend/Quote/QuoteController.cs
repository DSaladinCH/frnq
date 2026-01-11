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
	public async Task<ApiResponse> Search([FromQuery] string query, [FromQuery] string providerId = "yahoo-finance", CancellationToken cancellationToken = default)
		=> await quoteManagement.SearchQuotesAsync(query, providerId, cancellationToken);

	// GET: api/quote/historical?symbol=AAPL&from=2024-01-01&to=2024-01-10
	[HttpGet("historical")]
	public async Task<ApiResponse> GetHistoricalPrices([FromQuery] string symbol, [ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime from, [ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime to, [FromQuery] string providerId = "yahoo-finance", CancellationToken cancellationToken = default)
		=> await quoteManagement.GetHistoricalPricesAsync(providerId, symbol, from, to, cancellationToken);

	[HttpPut("{quoteId}/customName")]
	public async Task<ApiResponse> UpdateCustomName([FromRoute] int quoteId, [FromBody] CustomNameDto customName, CancellationToken cancellationToken)
		=> await quoteManagement.UpdateCustomNameAsync(quoteId, customName.CustomName, cancellationToken);

	[HttpDelete("{quoteId}/customName")]
	public async Task<ApiResponse> DeleteCustomName([FromRoute] int quoteId, CancellationToken cancellationToken)
		=> await quoteManagement.DeleteCustomNameAsync(quoteId, cancellationToken);
}