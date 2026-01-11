namespace DSaladin.Frnq.Api.Quote.Providers;

public interface IFinanceProvider
{
	/// <summary>
	/// Gets the internal identifier for the provider.
	/// </summary>
	string InternalId { get; }

	/// <summary>
	/// Gets the name of the provider.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Fetches a stock quote for the given symbol.
	/// </summary>
	/// <param name="symbol">The stock symbol to fetch the quote for.</param>
	/// <returns>A task that represents the asynchronous operation, containing the stock quote model.</returns>
	Task<QuoteModel?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);

	/// <summary>
	/// Searches for a stock quote based on the provided query.
	/// </summary>
	/// <param name="query">The search query, which can be a stock symbol or company name.</param
	/// <returns>A task that represents the asynchronous operation, containing a list of matching stock quotes.</returns>
	Task<IEnumerable<QuoteModel>> SearchAsync(string query, CancellationToken cancellationToken = default);

	/// <summary>
	/// Fetches historical prices for a given symbol.
	/// </summary>
	/// <param name="symbol">The stock symbol to fetch historical prices for.</param>
	/// <param name="from">The start date for the historical data.</param>
	/// <param name="to">The end date for the historical data.</param>
	/// <returns>A task that represents the asynchronous operation, containing a list of historical prices.</returns>
	Task<IEnumerable<QuotePrice>> GetHistoricalPricesAsync(string symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}