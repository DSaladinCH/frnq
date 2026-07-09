namespace DSaladin.Frnq.Api.Forecast;

/// <summary>
/// Represents forecast data for a single day across all aggregation levels.
/// </summary>
public class ForecastDayDto
{
	/// <summary>
	/// The date for this forecast.
	/// </summary>
	public DateOnly Date { get; set; }

	/// <summary>
	/// The forecast band for the entire portfolio.
	/// </summary>
	public ForecastBand Portfolio { get; set; } = new();

	/// <summary>
	/// Forecast bands for each group. Includes groups with GroupId and their respective bands.
	/// </summary>
	public List<ForecastGroupDto> Groups { get; set; } = new();

	/// <summary>
	/// Forecast bands for each quote. Includes quotes with QuoteId and their respective bands.
	/// </summary>
	public List<ForecastQuoteDto> Quotes { get; set; } = new();
}