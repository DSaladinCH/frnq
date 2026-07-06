using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Forecast;

public class ForecastManagement(DatabaseContext databaseContext, AuthManagement authManagement)
{
	public async Task<ApiResponse<List<ForecastDayDto>>> CreateForecast(CancellationToken cancellationToken = default)
	{
		Guid userId = authManagement.GetCurrentUserId();

		// Get all investments for the user, grouped by quote, calculating net position (buy - sell)
		var investmentsByQuote = await databaseContext.Investments
			.AsNoTracking()
			.Where(i => i.UserId == userId && (i.Type == InvestmentType.Buy || i.Type == InvestmentType.Sell))
			.GroupBy(i => i.QuoteId)
			.Select(g => new
			{
				QuoteId = g.Key,
				TotalAmount = g.Sum(i => i.Type == InvestmentType.Buy ? i.Amount : -i.Amount)
			})
			.ToDictionaryAsync(i => i.QuoteId, i => i.TotalAmount, cancellationToken);

		if (investmentsByQuote.Count == 0)
			return ApiResponse.Create(new List<ForecastDayDto>(), System.Net.HttpStatusCode.OK);

		// Filter out quotes with zero or negative net positions
		var activePositions = investmentsByQuote.Where(kvp => kvp.Value > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		
		if (activePositions.Count == 0)
			return ApiResponse.Create(new List<ForecastDayDto>(), System.Net.HttpStatusCode.OK);

		int[] quoteIds = activePositions.Keys.OrderBy(id => id).ToArray();
		int quoteCount = quoteIds.Length;

		// Get latest quote price for each quote and group mappings
		var quotePriceAndGroupData = await databaseContext.Quotes
			.AsNoTracking()
			.Where(q => quoteIds.Contains(q.Id))
			.Select(q => new
			{
				q.Id,
				GroupId = q.Mappings
					.Where(m => m.UserId == userId)
					.Select(m => (int?)m.GroupId)
					.FirstOrDefault(),
				LatestPrice = q.Prices
					.OrderByDescending(p => p.Date)
					.Select(p => p.AdjustedClose)
					.FirstOrDefault()
			})
			.ToListAsync(cancellationToken);

		// Validate that all quotes have valid price data (not null, zero, or negative)
		if (quotePriceAndGroupData.Any(q => q.LatestPrice == null || q.LatestPrice <= 0))
			return ApiResponse.Create("Invalid data", "One or more quotes have invalid price data (null, zero, or negative)", System.Net.HttpStatusCode.BadRequest);

		// Build lookup dictionaries
		Dictionary<int, decimal> currentValueByQuote = new(quoteCount);
		Dictionary<int, int?> groupByQuote = new(quoteCount);

		foreach (var quoteData in quotePriceAndGroupData)
		{
			decimal totalAmount = activePositions[quoteData.Id];
			decimal latestPrice = quoteData.LatestPrice ?? 0; // Already validated above
			currentValueByQuote[quoteData.Id] = totalAmount * latestPrice;
			groupByQuote[quoteData.Id] = quoteData.GroupId;
		}

		// --- One DB round trip, no change tracking, projected to only what we need ---
		var priceRows = await databaseContext.QuotePrices
			.AsNoTracking()
			.Where(p => quoteIds.Contains(p.QuoteId))
			.Select(p => new { p.QuoteId, p.Date, p.AdjustedClose })
			.ToListAsync(cancellationToken);

		// Common dates = dates that exist for *every* quote. Computed in memory,
		// which avoids the giant IN clause the old commonDates.Contains(...) produced.
		DateTime[] dateOrder = priceRows
			.GroupBy(r => r.Date)
			.Where(g => g.Select(x => x.QuoteId).Distinct().Count() == quoteCount)
			.Select(g => g.Key)
			.OrderBy(d => d)
			.ToArray();

		int dateCount = dateOrder.Length;
		// NOTE: if you want to fail gracefully on thin history, guard here
		// (dateCount < 2 || dateCount - 1 < blockLength) and return an ApiResponse error.

		var dateIndex = new Dictionary<DateTime, int>(dateCount);
		for (int i = 0; i < dateCount; i++)
			dateIndex[dateOrder[i]] = i;

		var quoteIndex = new Dictionary<int, int>(quoteCount);
		for (int i = 0; i < quoteCount; i++)
			quoteIndex[quoteIds[i]] = i;

		// Price matrix [date][quote]. Rows kept as jagged arrays: cheap to reference
		// whole rows in the hot loop, allocated once.
		double[][] priceMatrix = new double[dateCount][];
		for (int r = 0; r < dateCount; r++)
			priceMatrix[r] = new double[quoteCount];

		foreach (var row in priceRows)
		{
			if (!dateIndex.TryGetValue(row.Date, out int r))
				continue; // not a common date

			priceMatrix[r][quoteIndex[row.QuoteId]] = (double)row.AdjustedClose;
		}

		for (int r = 0; r < dateCount; r++)
			for (int c = 0; c < quoteCount; c++)
				if (priceMatrix[r][c] <= 0)
					throw new InvalidOperationException(
						$"Invalid price {priceMatrix[r][c]} for quote {quoteIds[c]} on {dateOrder[r]:yyyy-MM-dd}");

		// Bootstrap on price RATIOS, not log returns. Cumulative product of ratios
		// == exp(sum of log returns), so we drop every Log() here AND every Exp()
		// in the hot loop (~10M exp calls gone).
		int returnCount = dateCount - 1;
		double[][] ratioMatrix = new double[returnCount][];
		for (int r = 0; r < returnCount; r++)
		{
			double[] rowRatios = new double[quoteCount];
			for (int c = 0; c < quoteCount; c++)
				rowRatios[c] = priceMatrix[r + 1][c] / priceMatrix[r][c];
			ratioMatrix[r] = rowRatios;
		}

		double[] currentValues = new double[quoteCount];
		for (int q = 0; q < quoteCount; q++)
			currentValues[q] = (double)currentValueByQuote[quoteIds[q]];

		// Dense group mapping: quote index -> group slot. Replaces the per-cell
		// dictionary lookups inside the aggregation.
		int?[] uniqueGroupIds = groupByQuote.Values.Distinct().ToArray();
		int groupCount = uniqueGroupIds.Length;

		var groupSlot = new Dictionary<int?, int>(groupCount);
		for (int i = 0; i < groupCount; i++)
			groupSlot[uniqueGroupIds[i]] = i;

		int[] groupOfQuote = new int[quoteCount];
		for (int q = 0; q < quoteCount; q++)
			groupOfQuote[q] = groupSlot[groupByQuote.GetValueOrDefault(quoteIds[q])];

		const int simulationCount = 2000;
		const int horizon = 252;   // 1 year of trading days
		const int blockLength = 10;
		int returnLen = ratioMatrix.Length;

		// Flat per-series storage, indexed [day * sims + sim] so all sims for a
		// given (series, day) are contiguous -> percentile extraction is an
		// in-place Array.Sort on a sub-range, zero temp allocations.
		int cellCount = horizon * simulationCount;
		double[] portfolioSeries = new double[cellCount];
		double[][] groupSeries = new double[groupCount][];
		for (int g = 0; g < groupCount; g++) groupSeries[g] = new double[cellCount];
		double[][] quoteSeries = new double[quoteCount][];
		for (int q = 0; q < quoteCount; q++) quoteSeries[q] = new double[cellCount];

		var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken };

		Parallel.For(
			0, simulationCount, parallelOptions,
			// thread-local buffers, allocated once per worker
			() => (rng: new Random(), running: new double[quoteCount], groupSums: new double[groupCount]),
			(sim, _, local) =>
			{
				var (rng, running, groupSums) = local;
				Array.Copy(currentValues, running, quoteCount);

				int blockPos = blockLength; // force a fresh block on day 0
				int startIndex = 0;

				for (int day = 0; day < horizon; day++)
				{
					if (blockPos == blockLength)
					{
						startIndex = rng.Next(0, returnLen - blockLength + 1);
						blockPos = 0;
					}

					double[] dayRatios = ratioMatrix[startIndex + blockPos];
					blockPos++;

					int idx = day * simulationCount + sim;
					double portfolioSum = 0;
					Array.Clear(groupSums, 0, groupCount);

					for (int q = 0; q < quoteCount; q++)
					{
						running[q] *= dayRatios[q];          // no Exp()
						double v = running[q];
						quoteSeries[q][idx] = v;
						portfolioSum += v;
						groupSums[groupOfQuote[q]] += v;     // O(1) bucket, single pass
					}

					portfolioSeries[idx] = portfolioSum;
					for (int g = 0; g < groupCount; g++)
						groupSeries[g][idx] = groupSums[g];
				}

				return local;
			},
			_ => { });

		DateOnly[] forecastDates = new DateOnly[horizon];
		DateOnly current = DateOnly.FromDateTime(DateTime.UtcNow);
		for (int i = 0; i < horizon; i++)
		{
			current = current.AddDays(1);
			while (current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
				current = current.AddDays(1);
			forecastDates[i] = current;
		}

		ForecastBand[] portfolioBands = ExtractBands(portfolioSeries, horizon, simulationCount);

		ForecastBand[][] groupBands = new ForecastBand[groupCount][];
		for (int g = 0; g < groupCount; g++)
			groupBands[g] = ExtractBands(groupSeries[g], horizon, simulationCount);

		ForecastBand[][] quoteBands = new ForecastBand[quoteCount][];
		for (int q = 0; q < quoteCount; q++)
			quoteBands[q] = ExtractBands(quoteSeries[q], horizon, simulationCount);

		List<ForecastDayDto> result = new(horizon);
		for (int day = 0; day < horizon; day++)
		{
			List<ForecastGroupDto> groupForecasts = new(groupCount);
			for (int g = 0; g < groupCount; g++)
				groupForecasts.Add(new ForecastGroupDto { GroupId = uniqueGroupIds[g], Band = groupBands[g][day] });

			List<ForecastQuoteDto> quoteForecasts = new(quoteCount);
			for (int q = 0; q < quoteCount; q++)
				quoteForecasts.Add(new ForecastQuoteDto { QuoteId = quoteIds[q], Band = quoteBands[q][day] });

			result.Add(new ForecastDayDto
			{
				Date = forecastDates[day],
				Portfolio = portfolioBands[day],
				Groups = groupForecasts,
				Quotes = quoteForecasts
			});
		}

		return ApiResponse.Create(result, System.Net.HttpStatusCode.OK);
	}

	/// <summary>
	/// Extracts (5th, 50th, 95th) percentile bands per day from a flat series laid out
	/// as [day * sims + sim]. Sorts each day-slice in place; no per-day allocation.
	/// </summary>
	private static ForecastBand[] ExtractBands(double[] series, int horizon, int sims)
	{
		var bands = new ForecastBand[horizon];
		int lowerIdx = (int)(sims * 0.05);
		int medianIdx = sims / 2;
		int upperIdx = (int)(sims * 0.95);

		for (int day = 0; day < horizon; day++)
		{
			int off = day * sims;
			Array.Sort(series, off, sims);
			bands[day] = new ForecastBand
			{
				Lower = series[off + lowerIdx],
				Median = series[off + medianIdx],
				Upper = series[off + upperIdx]
			};
		}

		return bands;
	}
}