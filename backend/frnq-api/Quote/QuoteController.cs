using DSaladin.Frnq.Api.ModelBinders;
using DSaladin.Frnq.Api.Quote.Providers;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Quote;

[ApiController]
[Route("api/[controller]")]
public class QuoteController(IFinanceProvider financeProvider, DatabaseProvider databaseProvider) : ControllerBase
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
        QuoteModel? quote = await databaseProvider.GetQuoteAsync(financeProvider.InternalId, symbol);
        List<QuotePrice> dbPrices = [];

        if (quote is null)
            await databaseProvider.AddOrUpdateQuoteAsync(await financeProvider.GetQuoteAsync(symbol));

        if (quote is not null)
            dbPrices = (await databaseProvider.GetHistoricalPricesAsync(symbol, from.AddDays(-1), to.AddDays(1))).ToList();

        // Find earliest and latest in the db result
        QuotePrice? earliestDb = dbPrices.OrderBy(p => p.Date).FirstOrDefault();
        QuotePrice? latestDb = dbPrices.OrderByDescending(p => p.Date).FirstOrDefault();

        DateTime? fetchStart = null;
        DateTime? fetchEnd = null;

        if (earliestDb == null || earliestDb.Date > from)
            fetchStart = from;
        else
            fetchStart = null;

        if (latestDb == null || latestDb.Date < to)
            fetchEnd = to;
        else
            fetchEnd = null;

        // If we need to fetch any range, combine into one request
        if ((fetchStart.HasValue && fetchEnd.HasValue) || (fetchStart.HasValue && !fetchEnd.HasValue) || (!fetchStart.HasValue && fetchEnd.HasValue))
        {
            DateTime start = fetchStart ?? (earliestDb != null ? earliestDb.Date : from);
            DateTime end = fetchEnd ?? (latestDb != null ? latestDb.Date : to);
            
            if (start <= end)
            {
                IEnumerable<QuotePrice> fetched = await financeProvider.GetHistoricalPricesAsync(symbol, start, end);

                if (fetched.Any())
                {
                    foreach (QuotePrice p in fetched)
                        p.ProviderId = quote?.ProviderId ?? financeProvider.InternalId;

                    await databaseProvider.AddOrUpdateQuotePricesAsync(fetched.ToList());
                    dbPrices.AddRange(fetched);
                }
            }
        }

        List<QuotePrice> allCombined = dbPrices
            .GroupBy(p => p.Date)
            .Select(g => g.First())
            .Where(p => p.Date >= from && p.Date <= to)
            .OrderBy(p => p.Date)
            .ToList();

        return Ok(allCombined);
    }
}