using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Quote.Providers;

public class DatabaseProvider(DatabaseContext databaseContext)
{
	public string InternalId => "database";
	public string Name => "Database";

	public async Task<QuoteModel?> GetQuoteAsync(int quoteId, CancellationToken cancellationToken)
	{
		return await databaseContext.Quotes
			.FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);
	}

	public async Task<QuoteModel?> GetQuoteAsync(string providerId, string symbol, CancellationToken cancellationToken)
	{
		return await databaseContext.Quotes
			.FirstOrDefaultAsync(q => q.ProviderId == providerId && q.Symbol == symbol, cancellationToken);
	}

	public async Task<IEnumerable<QuoteModel>> SearchAsync(string query, CancellationToken cancellationToken)
	{
		return await databaseContext.Quotes
			.AsNoTracking()
			.Where(q => q.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || q.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase))
			.ToListAsync(cancellationToken);
	}

	public async Task<IEnumerable<QuotePrice>> GetHistoricalPricesAsync(int quoteId, DateTime from, DateTime to, CancellationToken cancellationToken)
	{
		return await databaseContext.QuotePrices
			.AsNoTracking()
			.Where(qp => qp.QuoteId == quoteId && qp.Date >= from && qp.Date <= to)
			.OrderBy(qp => qp.Date)
			.ToListAsync(cancellationToken);
	}

	public async Task<QuoteModel?> AddOrUpdateQuoteAsync(QuoteModel? quote, CancellationToken cancellationToken)
	{
		if (quote is null)
			return null;

		QuoteModel? existing = await GetQuoteAsync(quote.ProviderId, quote.Symbol, cancellationToken);

		if (existing is not null)
		{
			quote.Id = existing.Id;
			quote.LastUpdatedPrices = existing.LastUpdatedPrices;
			databaseContext.Entry(existing).CurrentValues.SetValues(quote);
		}
		else
			await databaseContext.Quotes.AddAsync(quote, cancellationToken);

		await databaseContext.SaveChangesAsync(cancellationToken);
		return quote;
	}

	public async Task AddOrUpdateQuotePricesAsync(IEnumerable<QuotePrice> prices, CancellationToken cancellationToken)
	{
		if (!prices.Any())
			return;

		QuoteModel? quote = await GetQuoteAsync(prices.First().QuoteId, cancellationToken);

		if (quote is null)
			return;

		// Get existing prices for the same dates (date only, ignoring time)
		List<DateTime> priceDates = prices.Select(x => x.Date.Date).Distinct().ToList();
		List<QuotePrice> existingPrices = await databaseContext.QuotePrices
			.Where(p => p.QuoteId == quote.Id && priceDates.Contains(p.Date.Date))
			.ToListAsync(cancellationToken);

		// Group existing prices by date (date only) to handle multiple entries per day
		Dictionary<DateTime, List<QuotePrice>> existingByDate = existingPrices
			.GroupBy(p => p.Date.Date)
			.ToDictionary(g => g.Key, g => g.ToList());

		List<QuotePrice> newPrices = [];

		foreach (QuotePrice price in prices)
		{
			DateTime dateOnly = price.Date.Date;

			if (existingByDate.TryGetValue(dateOnly, out List<QuotePrice>? existingForDate))
			{
				// Find the existing price with the latest time for this date
				QuotePrice? latestExisting = existingForDate.OrderByDescending(p => p.Date).FirstOrDefault();

				if (latestExisting != null)
				{
					// Only update if the new price has a newer time than the existing latest
					if (price.Date > latestExisting.Date)
					{
						// Remove all existing prices for this date and add the new one
						databaseContext.QuotePrices.RemoveRange(existingForDate);
						newPrices.Add(price);
					}
					// If new time is not newer, skip this price (don't update)
				}
				else
				{
					// This shouldn't happen, but add as new if no existing found
					newPrices.Add(price);
				}
			}
			else
			{
				// No existing price for this date, add as new
				newPrices.Add(price);
			}
		}

		if (newPrices.Count > 0)
			await databaseContext.QuotePrices.AddRangeAsync(newPrices, cancellationToken);

		if (string.IsNullOrEmpty(quote.Currency) && prices.First().Currency is not null)
			quote.Currency = prices.First().Currency;

		quote.LastUpdatedPrices = DateTime.UtcNow;
		databaseContext.Entry(quote).State = EntityState.Modified;

		await databaseContext.SaveChangesAsync(cancellationToken);
	}
}