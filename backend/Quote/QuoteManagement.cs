using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote.Providers;
using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Quote;

public class QuoteManagement(AuthManagement authManagement, DatabaseContext databaseContext, ProviderRegistry registry, DatabaseProvider databaseProvider)
{
	private readonly Guid userId = authManagement.GetCurrentUserId();

	public async Task<ApiResponse<List<QuoteModel>>> SearchQuotesAsync(string query, string providerId = "yahoo-finance", CancellationToken cancellationToken = default)
	{
		IFinanceProvider? financeProvider = registry.GetProvider(providerId);

		if (financeProvider is null)
			return ApiResponse.Create(ResponseCodes.Quote.ProviderNotFound, System.Net.HttpStatusCode.BadRequest);

		IEnumerable<QuoteModel> results = await financeProvider.SearchAsync(query, cancellationToken);

		// Ignoring CA1862 because of EF Core query translation limitations
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
		// Flavor the results with quotes from the database that match the query (quote symbol, name or custom name)
		// The results should use custom names if they exist
		// and the results should be above the ones from the external provider

		List<QuoteModel> dbQuotes = await databaseContext.Quotes
			.AsNoTracking()
			.Include(q => q.Names.Where(n => n.UserId == userId))
			.Where(q => q.ProviderId == providerId && (q.Symbol.ToLower().Contains(query.ToLower()) || q.Name.ToLower().Contains(query.ToLower()) ||
				q.Names.Any(n => n.UserId == userId && n.CustomName.ToLower().Contains(query.ToLower()))))
			.ToListAsync(cancellationToken);
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

		// Use custom names from the database quotes if they exist
		foreach (QuoteModel dbQuote in dbQuotes)
		{
			QuoteName? customName = dbQuote.Names.FirstOrDefault();

			if (customName is not null)
				dbQuote.Name = customName.CustomName;
		}

		// Filter external results to exclude symbols already present in dbQuotes
		HashSet<string> existingSymbols = [.. dbQuotes.Select(q => q.Symbol)];
		IEnumerable<QuoteModel> externalResults = results.Where(r => !existingSymbols.Contains(r.Symbol));
		results = dbQuotes.Concat(externalResults);
		
		return ApiResponse.Create(results.ToList(), System.Net.HttpStatusCode.OK);
	}

	public async Task<QuoteModel?> GetQuoteAsync(int quoteId, CancellationToken cancellationToken)
	{
		return await databaseContext.Quotes.FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);
	}

	public async Task<QuoteModel?> GetQuoteAsync(string providerId, string symbol, CancellationToken cancellationToken)
	{
		return await databaseContext.Quotes.FirstOrDefaultAsync(q => q.ProviderId == providerId && q.Symbol == symbol, cancellationToken);
	}

	public async Task<QuoteModel?> GetQuoteAsync(InvestmentDto investment, CancellationToken cancellationToken)
	{
		if (investment.QuoteId > 0)
			return await GetQuoteAsync(investment.QuoteId, cancellationToken);

		if (!string.IsNullOrEmpty(investment.ProviderId) && !string.IsNullOrEmpty(investment.QuoteSymbol))
			return await GetQuoteAsync(investment.ProviderId, investment.QuoteSymbol, cancellationToken);

		return null;
	}

	public async Task<bool> QuoteExistsAsync(int quoteId, CancellationToken cancellationToken)
	{
		return await databaseContext.Quotes.AnyAsync(q => q.Id == quoteId, cancellationToken);
	}

	public async Task<bool> QuoteExistsAsync(string providerId, string symbol, CancellationToken cancellationToken)
	{
		return await databaseContext.Quotes.AnyAsync(q => q.ProviderId == providerId && q.Symbol == symbol, cancellationToken);
	}

	public async Task<QuoteModel> GetOrAddQuoteAsync(string providerId, string symbol, CancellationToken cancellationToken)
	{
		QuoteModel? quote = await databaseProvider.GetQuoteAsync(providerId, symbol, cancellationToken);
		if (quote is not null)
			return quote;

		IFinanceProvider? financeProvider = registry.GetProvider(providerId) ?? throw new ArgumentException($"Finance provider '{providerId}' not found.", nameof(providerId));
		quote = await financeProvider.GetQuoteAsync(symbol) ?? throw new ArgumentException($"Quote with symbol '{symbol}' not found.", nameof(symbol));

		await databaseProvider.AddOrUpdateQuoteAsync(quote, cancellationToken);
		return quote;
	}

	public async Task<ApiResponse<List<QuotePrice>>> GetHistoricalPricesAsync(string providerId, string symbol, DateTime from, DateTime to, CancellationToken cancellationToken)
	{
		if (from > to)
			throw new ArgumentException("The 'from' date cannot be later than the 'to' date.", nameof(from));

		QuoteModel quote = await GetOrAddQuoteAsync(providerId, symbol, cancellationToken);
		return await GetHistoricalPricesAsync(quote.Id, from, to, cancellationToken);
	}

	public async Task<ApiResponse<List<QuotePrice>>> GetHistoricalPricesAsync(int quoteId, DateTime from, DateTime to, CancellationToken cancellationToken)
	{
		QuoteModel? quote = await databaseProvider.GetQuoteAsync(quoteId, cancellationToken) ?? throw new ArgumentException($"Quote with id '{quoteId}' not found.", nameof(quoteId));

		List<QuotePrice> dbPrices = await databaseContext.QuotePrices
			.Where(p => p.QuoteId == quoteId && p.Date >= from && p.Date <= to)
			.ToListAsync(cancellationToken);
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

			dbPrices.AddRange(await GetExternalHistoricalPricesAsync(quote, fetchStart, fetchEnd, cancellationToken));
		}

		List<QuotePrice> allCombined = [.. dbPrices
			.GroupBy(p => p.Date)
			.Select(g => g.First())
			.Where(p => p.Date >= from && p.Date <= to)
			.OrderBy(p => p.Date)];

		return ApiResponse.Create(allCombined, System.Net.HttpStatusCode.OK);
	}

	private async Task<IEnumerable<QuotePrice>> GetExternalHistoricalPricesAsync(QuoteModel quote, DateTime? fetchStart, DateTime? fetchEnd, CancellationToken cancellationToken)
	{
		// If we need to fetch any range, combine into one request
		if ((fetchStart.HasValue && fetchEnd.HasValue) || (fetchStart.HasValue && !fetchEnd.HasValue) || (!fetchStart.HasValue && fetchEnd.HasValue))
		{
			DateTime start = fetchStart ?? quote.LastUpdatedPrices;
			DateTime end = fetchEnd ?? DateTime.UtcNow;

			if (start > end)
				return [];

			IFinanceProvider? financeProvider = registry.GetProvider(quote.ProviderId) ?? throw new ArgumentException($"Finance provider '{quote.ProviderId}' not found.", nameof(quote.ProviderId));
			IEnumerable<QuotePrice> fetched = await financeProvider.GetHistoricalPricesAsync(quote.Symbol, start, end, cancellationToken);

			if (!fetched.Any())
				return [];

			foreach (QuotePrice p in fetched)
				p.QuoteId = quote.Id;

			await databaseProvider.AddOrUpdateQuotePricesAsync(fetched.ToList(), cancellationToken);
			return fetched;
		}

		return [];
	}

	public async Task<ApiResponse> UpdateCustomNameAsync(int quoteId, string customName, CancellationToken cancellationToken)
	{
		QuoteModel? quote = await databaseContext.Quotes.FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);

		if (quote is null)
			return ApiResponses.NotFound404;

		QuoteName? existingName = await databaseContext.QuoteNames.FirstOrDefaultAsync(n => n.UserId == userId && n.QuoteId == quoteId, cancellationToken);
		if (existingName is null)
		{
			existingName = new QuoteName
			{
				UserId = userId,
				QuoteId = quoteId,
				CustomName = customName
			};
			await databaseContext.QuoteNames.AddAsync(existingName, cancellationToken);
		}
		else
			existingName.CustomName = customName;

		await databaseContext.SaveChangesAsync(cancellationToken);
		return ApiResponses.NoContent204;
	}

	public async Task<ApiResponse> DeleteCustomNameAsync(int quoteId, CancellationToken cancellationToken)
	{
		QuoteModel? quote = await databaseContext.Quotes.FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);

		if (quote is null)
			return ApiResponses.NotFound404;

		QuoteName? existingName = await databaseContext.QuoteNames.FirstOrDefaultAsync(n => n.UserId == userId && n.QuoteId == quoteId, cancellationToken);

		if (existingName is not null)
		{
			databaseContext.QuoteNames.Remove(existingName);
			await databaseContext.SaveChangesAsync(cancellationToken);
		}

		return ApiResponses.NoContent204;
	}
}