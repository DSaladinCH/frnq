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
        QuoteModel? quote = await databaseProvider.GetQuoteAsync(symbol);
        List<QuotePrice> dbPrices = [];

        if (quote is not null)
            dbPrices = (await databaseProvider.GetHistoricalPricesAsync(symbol, from.AddDays(-1), to.AddDays(1))).ToList();

        // Find earliest and latest in the db result
        QuotePrice? earliestDb = dbPrices.OrderBy(p => p.Date).FirstOrDefault();
        QuotePrice? latestDb = dbPrices.OrderByDescending(p => p.Date).FirstOrDefault();

        DateTime effectiveFrom = from;
        DateTime effectiveTo = to;

        if (earliestDb != null && earliestDb.Date <= from)
            effectiveFrom = from;
        else if (earliestDb != null && earliestDb.Date > from)
            effectiveFrom = earliestDb.Date;

        if (latestDb != null && latestDb.Date >= to)
            effectiveTo = to;
        else if (latestDb != null && latestDb.Date < to)
            effectiveTo = latestDb.Date;

        // If the effective range is empty, just return what we have
        if (effectiveFrom > effectiveTo)
        {
            List<QuotePrice> filtered = dbPrices.Where(p => p.Date >= from && p.Date <= to).OrderBy(p => p.Date).ToList();
            return Ok(filtered);
        }

        // Find all unique DateTime points in the effective range
        HashSet<DateTime> dbDateTimes = dbPrices.Select(p => p.Date).ToHashSet();
        HashSet<DateTime> expectedDateTimes = dbPrices.Where(p => p.Date >= effectiveFrom && p.Date <= effectiveTo).Select(p => p.Date).ToHashSet();
        // If there are gaps in the requested range, we can't know the exact missing points without a fixed interval, so we fetch the whole missing range
        bool hasGap = (effectiveTo - effectiveFrom).TotalSeconds > 0 && expectedDateTimes.Count == 0;

        List<QuotePrice> providerPrices = [];

        if (hasGap)
        {
            IEnumerable<QuotePrice> fetched = await financeProvider.GetHistoricalPricesAsync(symbol, effectiveFrom, effectiveTo);
            providerPrices.AddRange(fetched);

            if (providerPrices.Count > 0)
            {
                foreach (var p in providerPrices)
                    p.ProviderId = quote?.ProviderId ?? financeProvider.InternalId;

                await databaseProvider.AddOrUpdateQuotePricesAsync(providerPrices);
            }
        }

        List<QuotePrice> allCombined = dbPrices.Concat(providerPrices)
            .GroupBy(p => p.Date)
            .Select(g => g.First())
            .Where(p => p.Date >= from && p.Date <= to)
            .OrderBy(p => p.Date)
            .ToList();

        return Ok(allCombined);
    }
}