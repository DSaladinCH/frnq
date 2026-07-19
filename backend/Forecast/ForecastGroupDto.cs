namespace DSaladin.Frnq.Api.Forecast;

/// <summary>
/// Represents forecast statistics for a specific group.
/// </summary>
public class ForecastGroupDto
{
	/// <summary>
	/// The ID of the group. Null if this is the ungrouped (portfolio-level) forecast.
	/// </summary>
	public int? GroupId { get; set; }

	/// <summary>
	/// The forecast band (median, lower, upper percentiles) for this group.
	/// </summary>
	public ForecastBand Band { get; set; } = new();
}
