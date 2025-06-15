using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Quote.Providers;

public class DatabaseProvider(DatabaseContext databaseContext) : IFinanceProvider
{
    public string InternalId => "database";
    public string Name => "Database";

    public async Task<QuoteModel?> GetQuoteAsync(string symbol)
    {
        return await databaseContext.Quotes
            .FirstOrDefaultAsync(q => q.Symbol == symbol);
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

    public async Task<IEnumerable<QuotePrice>> GetHistoricalPricesAsync(string symbol, DateTime from, DateTime to)
    {
        return await databaseContext.QuotePrices
            .Where(qp => qp.Symbol == symbol && qp.Date >= from && qp.Date <= to)
            .OrderBy(qp => qp.Date)
            .ToListAsync();
    }

    public async Task AddOrUpdateQuoteAsync(QuoteModel? quote)
    {
        if (quote is null)
            return;

        QuoteModel? existing = await databaseContext.Quotes.FindAsync(quote.ProviderId, quote.Symbol);

        if (existing is null)
            await databaseContext.Quotes.AddAsync(quote);
        else
            databaseContext.Entry(existing).CurrentValues.SetValues(quote);

        await databaseContext.SaveChangesAsync();
    }

    public async Task AddOrUpdateQuotePricesAsync(IEnumerable<QuotePrice> prices)
    {
        if (!prices.Any())
            return;

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