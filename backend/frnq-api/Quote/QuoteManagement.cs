using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote.Providers;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Quote;

public class QuoteManagement(AuthManagement authManagement, DatabaseContext databaseContext, ProviderRegistry registry, DatabaseProvider databaseProvider)
{
    private readonly Guid userId = authManagement.GetCurrentUserId();

    public async Task<QuoteModel?> GetQuoteAsync(int quoteId)
    {
        return await databaseContext.Quotes.FirstOrDefaultAsync(q => q.Id == quoteId);
    }

    public async Task<QuoteModel?> GetQuoteAsync(string providerId, string symbol)
    {
        return await databaseContext.Quotes.FirstOrDefaultAsync(q => q.ProviderId == providerId && q.Symbol == symbol);
    }

    public async Task<QuoteModel?> GetQuoteAsync(InvestmentRequest investment)
    {
        if (investment.QuoteId > 0)
            return await GetQuoteAsync(investment.QuoteId);

        if (!string.IsNullOrEmpty(investment.ProviderId) && !string.IsNullOrEmpty(investment.QuoteSymbol))
            return await GetQuoteAsync(investment.ProviderId, investment.QuoteSymbol);

        return null;
    }

    public async Task<bool> QuoteExistsAsync(int quoteId)
    {
        return await databaseContext.Quotes.AnyAsync(q => q.Id == quoteId);
    }

    public async Task<bool> QuoteExistsAsync(string providerId, string symbol)
    {
        return await databaseContext.Quotes.AnyAsync(q => q.ProviderId == providerId && q.Symbol == symbol);
    }

    public async Task<QuoteModel> GetOrAddQuoteAsync(string providerId, string symbol)
    {
        QuoteModel? quote = await databaseProvider.GetQuoteAsync(providerId, symbol);
        if (quote is not null)
            return quote;

        IFinanceProvider? financeProvider = registry.GetProvider(providerId) ?? throw new ArgumentException($"Finance provider '{providerId}' not found.", nameof(providerId));
        quote = await financeProvider.GetQuoteAsync(symbol) ?? throw new ArgumentException($"Quote with symbol '{symbol}' not found.", nameof(symbol));

        await databaseProvider.AddOrUpdateQuoteAsync(quote);
        return quote;
    }

    public async Task<List<QuotePrice>> GetHistoricalPricesAsync(string providerId, string symbol, DateTime from, DateTime to)
    {
        if (from > to)
            throw new ArgumentException("The 'from' date cannot be later than the 'to' date.", nameof(from));

        QuoteModel quote = await GetOrAddQuoteAsync(providerId, symbol);
        return await GetHistoricalPricesAsync(quote.Id, from, to);
    }

    public async Task<List<QuotePrice>> GetHistoricalPricesAsync(int quoteId, DateTime from, DateTime to)
    {
        QuoteModel? quote = await databaseProvider.GetQuoteAsync(quoteId) ?? throw new ArgumentException($"Quote with id '{quoteId}' not found.", nameof(quoteId));
        
        List<QuotePrice> dbPrices = await databaseContext.QuotePrices
            .Where(p => p.QuoteId == quoteId && p.Date >= from && p.Date <= to)
            .ToListAsync();
        
#if DEBUG
        if (quote.LastUpdatedPrices < DateTime.UtcNow.AddMinutes(-60))
#else
        if (quote.LastUpdatedPrices < DateTime.UtcNow.AddMinutes(-1))
#endif
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

        List<QuotePrice> allCombined = [.. dbPrices
            .GroupBy(p => p.Date)
            .Select(g => g.First())
            .Where(p => p.Date >= from && p.Date <= to)
            .OrderBy(p => p.Date)];

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

            IFinanceProvider? financeProvider = registry.GetProvider(quote.ProviderId) ?? throw new ArgumentException($"Finance provider '{quote.ProviderId}' not found.", nameof(quote.ProviderId));
            IEnumerable<QuotePrice> fetched = await financeProvider.GetHistoricalPricesAsync(quote.Symbol, start, end);

            if (!fetched.Any())
                return [];

            foreach (QuotePrice p in fetched)
                p.QuoteId = quote.Id;

            await databaseProvider.AddOrUpdateQuotePricesAsync(fetched.ToList());
            return fetched;
        }

        return [];
    }

    public async Task<bool> UpdateCustomNameAsync(int quoteId, string customName)
    {
        QuoteModel? quote = await databaseContext.Quotes.FirstOrDefaultAsync(q => q.Id == quoteId);

        if (quote is null)
            return false;

        QuoteName? existingName = await databaseContext.QuoteNames.FirstOrDefaultAsync(n => n.UserId == userId && n.QuoteId == quoteId);
        if (existingName is null)
        {
            existingName = new QuoteName
            {
                UserId = userId,
                QuoteId = quoteId,
                CustomName = customName
            };
            databaseContext.QuoteNames.Add(existingName);
        }
        else
            existingName.CustomName = customName;

        await databaseContext.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> DeleteCustomNameAsync(int quoteId)
    {
        QuoteModel? quote = await databaseContext.Quotes.FirstOrDefaultAsync(q => q.Id == quoteId);

        if (quote is null)
            return false;

        QuoteName? existingName = await databaseContext.QuoteNames.FirstOrDefaultAsync(n => n.UserId == userId && n.QuoteId == quoteId);

        if (existingName is not null)
        {
            databaseContext.QuoteNames.Remove(existingName);
            await databaseContext.SaveChangesAsync();
        }

        return true;
    }
}