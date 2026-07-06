using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Position;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Forecast;

public class ForecastManagement(PositionManagement positionManagement, DatabaseContext databaseContext, AuthManagement authManagement)
{
	public async Task<ApiResponse> CreateForecast(CancellationToken cancellationToken = default)
	{
		ApiResponse<PositionsResponse> positions = await positionManagement.GetPositionsAsync(DateTime.UtcNow, DateTime.UtcNow, cancellationToken);

		if (positions.Failed)
			return ApiResponse.Create(positions.Code, positions.Description, positions.StatusCode);

		List<PositionSnapshot> snapshots = positions.Value.Snapshots
			.Where(s => s.Amount > 0)
			.ToList();

		int[] quoteIds = snapshots.Select(s => s.QuoteId).OrderBy(id => id).ToArray();
		Dictionary<int, decimal> currentValueByQuote = snapshots.ToDictionary(s => s.QuoteId, s => s.CurrentValue);

		// Build QuoteId -> GroupId mapping from the quotes data
		Dictionary<int, int?> groupByQuote = new();
		foreach (QuoteViewDto quote in positions.Value.Quotes)
		{
			groupByQuote[quote.Id] = quote.Group?.Id;
		}

		IEnumerable<IQueryable<DateTime>> dateQueries = quoteIds.Select(id => databaseContext.QuotePrices.Where(p => p.QuoteId == id).Select(p => p.Date));

		List<DateTime> commonDates = await dateQueries
			.Aggregate((a, b) => a.Intersect(b))
			.OrderBy(d => d)
			.ToListAsync(cancellationToken);

		List<QuotePrice> rows = await databaseContext.QuotePrices
			.Where(p => quoteIds.Contains(p.QuoteId) && commonDates.Contains(p.Date))
			.ToListAsync(cancellationToken);

		DateTime[] dateOrder = commonDates.OrderBy(d => d).ToArray();

		// Temporary lookup tables for building the price matrix
		Dictionary<DateTime, int> dateIndex = dateOrder.Select((d, i) => (d, i)).ToDictionary(x => x.d, x => x.i);
		Dictionary<int, int> quoteIndex = quoteIds.Select((q, i) => (q, i)).ToDictionary(x => x.q, x => x.i);

		double[][] priceMatrix = dateOrder.Select(_ => new double[quoteIds.Length]).ToArray();

		foreach (QuotePrice row in rows)
		{
			int r = dateIndex[row.Date];
			int c = quoteIndex[row.QuoteId];
			priceMatrix[r][c] = (double)row.AdjustedClose;
		}

		int rowCount = priceMatrix.Length - 1;
		int colCount = quoteIds.Length;
		double[][] returnMatrix = new double[rowCount][];

		// Check for invalid prices (<= 0) in the price matrix (TODO: Handle without exception)
		for (int i = 0; i < priceMatrix.Length; i++)
		{
			for (int c = 0; c < quoteIds.Length; c++)
			{
				if (priceMatrix[i][c] <= 0)
				{
					throw new InvalidOperationException(
						$"Invalid price {priceMatrix[i][c]} for quote {quoteIds[c]} on {dateOrder[i]:yyyy-MM-dd}");
				}
			}
		}

		for (int i = 0; i < rowCount; i++)
		{
			returnMatrix[i] = new double[colCount];

			for (int c = 0; c < colCount; c++)
				returnMatrix[i][c] = Math.Log(priceMatrix[i + 1][c] / priceMatrix[i][c]);
		}

		double[] currentValues = quoteIds.Select(id => (double)currentValueByQuote[id]).ToArray();

		int simulationCount = 2000;
		double[][][] allQuoteValues = new double[simulationCount][][]; // [simulation][day][quote]
		Random random = new Random();

		int horizon = 252; // 1 year of trading days
		// int horizon = 504; // 2 year of trading days
		// int horizon = 2520; // 10 year of trading days
		int blockLength = 10; // Length of blocks for bootstrapping

		for (int sim = 0; sim < simulationCount; sim++)
		{
			double[][] bootstrapPath = GenerateBootstrapPath(returnMatrix, horizon, blockLength, random: random);
			double[][] valuePath = BuildValueTrajectory(currentValues, bootstrapPath);
			allQuoteValues[sim] = valuePath;
		}

		// Calculate forecasts for all aggregation levels: portfolio, groups, and quotes
		DateOnly[] forecastDates = new DateOnly[horizon];
		DateOnly current = DateOnly.FromDateTime(DateTime.UtcNow);

		for (int i = 0; i < horizon; i++)
		{
			current = current.AddDays(1);
			while (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday)
				current = current.AddDays(1);

			forecastDates[i] = current;
		}

		// Get unique groups (including null for ungrouped quotes)
		HashSet<int?> uniqueGroups = new(groupByQuote.Values);

		List<ForecastDayDto> result = new();

		for (int day = 0; day < horizon; day++)
		{
			// Compute portfolio total per simulation
			double[] portfolioValuesPerSim = new double[simulationCount];
			for (int sim = 0; sim < simulationCount; sim++)
			{
				double sum = 0;
				for (int q = 0; q < quoteIds.Length; q++)
				{
					sum += allQuoteValues[sim][day][q];
				}
				portfolioValuesPerSim[sim] = sum;
			}
			ForecastBand portfolioBand = GetPercentiles(portfolioValuesPerSim);

			// Compute per-group totals per simulation
			List<ForecastGroupDto> groupForecasts = new();
			foreach (int? groupId in uniqueGroups)
			{
				double[] groupValuesPerSim = new double[simulationCount];
				for (int sim = 0; sim < simulationCount; sim++)
				{
					double sum = 0;
					for (int q = 0; q < quoteIds.Length; q++)
					{
						if (groupByQuote[quoteIds[q]] == groupId)
						{
							sum += allQuoteValues[sim][day][q];
						}
					}
					groupValuesPerSim[sim] = sum;
				}
				ForecastBand groupBand = GetPercentiles(groupValuesPerSim);
				groupForecasts.Add(new ForecastGroupDto
				{
					GroupId = groupId ?? 0,
					Band = groupBand
				});
			}

			// Compute per-quote percentiles
			List<ForecastQuoteDto> quoteForecasts = new();
			for (int q = 0; q < quoteIds.Length; q++)
			{
				double[] valuesThisQuote = new double[simulationCount];
				for (int sim = 0; sim < simulationCount; sim++)
				{
					valuesThisQuote[sim] = allQuoteValues[sim][day][q];
				}
				ForecastBand quoteBand = GetPercentiles(valuesThisQuote);
				quoteForecasts.Add(new ForecastQuoteDto
				{
					QuoteId = quoteIds[q],
					Band = quoteBand
				});
			}

			result.Add(new ForecastDayDto
			{
				Date = forecastDates[day],
				Portfolio = portfolioBand,
				Groups = groupForecasts,
				Quotes = quoteForecasts
			});
		}

		return ApiResponse.Create(result, System.Net.HttpStatusCode.OK);
	}

	/// <summary>
	/// Computes percentile bands (median, 5th percentile, 95th percentile) from an array of simulation values.
	/// </summary>
	/// <param name="values">Array of values from simulations (sorted or unsorted).</param>
	/// <returns>A ForecastBand with median, lower (5th percentile), and upper (95th percentile) values.</returns>
	private static ForecastBand GetPercentiles(double[] values)
	{
		Array.Sort(values);
		int medianIndex = values.Length / 2;
		int lowerIndex = (int)(values.Length * 0.05);
		int upperIndex = (int)(values.Length * 0.95);

		return new ForecastBand
		{
			Median = values[medianIndex],
			Lower = values[lowerIndex],
			Upper = values[upperIndex]
		};
	}

	private static double[][] GenerateBootstrapPath(double[][] returnMatrix, int horizon, int blockLength, Random random)
	{
		double[][] path = new double[horizon][];
		int filled = 0;

		while (filled < horizon)
		{
			int startIndex = random.Next(0, returnMatrix.Length - blockLength + 1);
			int length = horizon - filled < blockLength ? horizon - filled : blockLength;

			for (int i = 0; i < length; i++)
			{
				path[filled + i] = returnMatrix[startIndex + i];
			}

			filled += length;
		}

		return path;
	}

	private static double[][] BuildValueTrajectory(double[] currentValues, double[][] bootstrapPath)
	{
		int horizon = bootstrapPath.Length;
		int quoteCount = currentValues.Length;
		double[][] valuePath = new double[horizon][];

		double[] runningValues = (double[])currentValues.Clone();

		for (int day = 0; day < horizon; day++)
		{
			valuePath[day] = new double[quoteCount];

			for (int q = 0; q < quoteCount; q++)
			{
				runningValues[q] *= Math.Exp(bootstrapPath[day][q]);
				valuePath[day][q] = runningValues[q];
			}
		}

		return valuePath;
	}
}