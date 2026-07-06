namespace DSaladin.Frnq.Api.Forecast;

/// <summary>
/// Represents forecast statistics for a specific quote.
/// </summary>
public class ForecastQuoteDto
{
	/// <summary>
	/// The ID of the quote.
	/// </summary>
	public int QuoteId { get; set; }

	/// <summary>
	/// The forecast band (median, lower, upper percentiles) for this quote.
	/// </summary>
	public ForecastBand Band { get; set; } = new();
}