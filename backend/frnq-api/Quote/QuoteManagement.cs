using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Quote;

public class QuoteManagement(DatabaseContext databaseContext)
{
    public async Task<bool> QuoteExistsAsync(string providerId, string symbol)
    {
        return await databaseContext.Quotes.AnyAsync(q => q.ProviderId == providerId && q.Symbol == symbol);
    }
}