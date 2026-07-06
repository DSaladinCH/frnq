namespace DSaladin.Frnq.Api.Forecast;

/// <summary>
/// Represents forecast percentile statistics (median, 5th percentile, 95th percentile)
/// computed from simulation results.
/// </summary>
public class ForecastBand
{
	/// <summary>
	/// The median (50th percentile) value from simulations.
	/// </summary>
	public double Median { get; set; }

	/// <summary>
	/// The lower bound (5th percentile) value from simulations.
	/// </summary>
	public double Lower { get; set; }

	/// <summary>
	/// The upper bound (95th percentile) value from simulations.
	/// </summary>
	public double Upper { get; set; }
}
