using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Quote.Providers;

public class DatabaseProvider(DatabaseContext databaseContext) : IFinanceProvider
{
    public string InternalId => "database";
    public string Name => "Database";

    public async Task<QuoteModel?> GetQuoteAsync(string symbol)
    {
        return await databaseContext.Quotes
            .Include(q => q.Prices)
            .FirstOrDefaultAsync(q => q.Symbol == symbol);
    }

    public async Task<IEnumerable<QuoteModel>> SearchAsync(string query)
    {
        return await databaseContext.Quotes
            .Where(q => q.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || q.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToListAsync();
    }

    public async Task<IEnumerable<QuotePrice>> GetHistoricalPricesAsync(string symbol, DateTime from, DateTime to)
    {
        return await databaseContext.QuotePrices
            .Where(qp => qp.Symbol == symbol && qp.Date >= from && qp.Date <= to)
            .OrderBy(qp => qp.Date)
            .ToListAsync();
    }

    public async Task AddOrUpdateQuotePricesAsync(IEnumerable<QuotePrice> prices)
    {
        foreach (QuotePrice price in prices)
        {
            QuotePrice? existing = await databaseContext.QuotePrices.FindAsync(price.ProviderId, price.Symbol, price.Date);

            if (existing == null)
                await databaseContext.QuotePrices.AddAsync(price);
            else
                databaseContext.Entry(existing).CurrentValues.SetValues(price);
        }

        await databaseContext.SaveChangesAsync();
    }
}