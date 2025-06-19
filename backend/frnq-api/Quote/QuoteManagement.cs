using DSaladin.Frnq.Api.Quote.Providers;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Quote;

public class QuoteManagement(DatabaseContext databaseContext, IFinanceProvider financeProvider, DatabaseProvider databaseProvider)
{
    public async Task<bool> QuoteExistsAsync(string providerId, string symbol)
    {
        return await databaseContext.Quotes.AnyAsync(q => q.ProviderId == providerId && q.Symbol == symbol);
    }

    public async Task<List<QuotePrice>> GetHistoricalPricesAsync(string providerId, string symbol, DateTime from, DateTime to)
    {
        QuoteModel? quote = await databaseProvider.GetQuoteAsync(financeProvider.InternalId, symbol);
        List<QuotePrice> dbPrices = [];

        if (quote is null)
            quote = await databaseProvider.AddOrUpdateQuoteAsync(await financeProvider.GetQuoteAsync(symbol));

        if (quote is null)
            throw new ArgumentException($"Quote with symbol '{symbol}' not found or could not be fetched.", nameof(symbol));

        dbPrices = (await databaseProvider.GetHistoricalPricesAsync(symbol, from.AddDays(-1), to.AddDays(1))).ToList();

        if (quote!.LastUpdatedPrices < DateTime.UtcNow)
        {
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

            dbPrices.AddRange(await GetExternalHistoricalPricesAsync(quote, fetchStart, fetchEnd));
        }

        List<QuotePrice> allCombined = dbPrices
            .GroupBy(p => p.Date)
            .Select(g => g.First())
            .Where(p => p.Date >= from && p.Date <= to)
            .OrderBy(p => p.Date)
            .ToList();

        return allCombined;
    }

    private async Task<IEnumerable<QuotePrice>> GetExternalHistoricalPricesAsync(QuoteModel quote, DateTime? fetchStart, DateTime? fetchEnd)
    {
        // If we need to fetch any range, combine into one request
        if ((fetchStart.HasValue && fetchEnd.HasValue) || (fetchStart.HasValue && !fetchEnd.HasValue) || (!fetchStart.HasValue && fetchEnd.HasValue))
        {
            DateTime start = fetchStart ?? quote.LastUpdatedPrices;
            DateTime end = fetchEnd ?? DateTime.UtcNow;

            if (start > end)
                return [];

            IEnumerable<QuotePrice> fetched = await financeProvider.GetHistoricalPricesAsync(quote.Symbol, start, end);

            if (!fetched.Any())
                return [];

            foreach (QuotePrice p in fetched)
                p.ProviderId = quote?.ProviderId ?? financeProvider.InternalId;

            await databaseProvider.AddOrUpdateQuotePricesAsync(fetched.ToList());
            return fetched;
        }

        return [];
    }
}