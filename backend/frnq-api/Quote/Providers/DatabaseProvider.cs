using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Quote.Providers;

public class DatabaseProvider(DatabaseContext databaseContext)
{
    public string InternalId => "database";
    public string Name => "Database";

    public async Task<QuoteModel?> GetQuoteAsync(int quoteId)
    {
        return await databaseContext.Quotes
            .FirstOrDefaultAsync(q => q.Id == quoteId);
    }

    public async Task<QuoteModel?> GetQuoteAsync(string providerId, string symbol)
    {
        return await databaseContext.Quotes
            .FirstOrDefaultAsync(q => q.ProviderId == providerId && q.Symbol == symbol);
    }

    public async Task<IEnumerable<QuoteModel>> SearchAsync(string query)
    {
        return await databaseContext.Quotes
            .Where(q => q.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || q.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToListAsync();
    }

    public async Task<IEnumerable<QuotePrice>> GetHistoricalPricesAsync(int quoteId, DateTime from, DateTime to)
    {
        return await databaseContext.QuotePrices
            .Where(qp => qp.QuoteId == quoteId && qp.Date >= from && qp.Date <= to)
            .OrderBy(qp => qp.Date)
            .ToListAsync();
    }

    public async Task<QuoteModel?> AddOrUpdateQuoteAsync(QuoteModel? quote)
    {
        if (quote is null)
            return null;

        QuoteModel? existing = await GetQuoteAsync(quote.ProviderId, quote.Symbol);

        if (existing is not null) {
            quote.Id = existing.Id;
            quote.LastUpdatedPrices = existing.LastUpdatedPrices;
            quote.GroupId = existing.GroupId;
            databaseContext.Entry(existing).CurrentValues.SetValues(quote);
        }
        else
            await databaseContext.Quotes.AddAsync(quote);

        await databaseContext.SaveChangesAsync();
        return quote;
    }

    public async Task AddOrUpdateQuotePricesAsync(IEnumerable<QuotePrice> prices)
    {
        if (!prices.Any())
            return;

        QuoteModel? quote = await GetQuoteAsync(prices.First().QuoteId);

        if (quote is null)
            return;

        foreach (QuotePrice price in prices)
        {
            QuotePrice? existing = await databaseContext.QuotePrices.FirstOrDefaultAsync(p => p.QuoteId == price.QuoteId && p.Date == price.Date);

            if (existing is not null) {
                price.Id = existing.Id;
                databaseContext.Entry(existing).CurrentValues.SetValues(price);
            }
            else
                await databaseContext.QuotePrices.AddAsync(price);
        }

        quote.LastUpdatedPrices = DateTime.UtcNow;
        databaseContext.Entry(quote).State = EntityState.Modified;

        await databaseContext.SaveChangesAsync();
    }
}