using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Forecast;

public class ForecastManagement(DatabaseContext databaseContext, AuthManagement authManagement)
{
	private const int simulationCount = 2000;
	// private const int horizon = 252;   // 1 year of trading days
	// private const int horizon = 504;   // 2 year of trading days
	private const int horizon = 2520;   // 10 year of trading days
	private const int blockLength = 10;
	private const double tradingDaysPerCalendarDay = 5.0 / 7.0;

	public async Task<ApiResponse<List<ForecastDayDto>>> CreateForecast(bool includeContributions = false, CancellationToken cancellationToken = default)
	{
		Guid userId = authManagement.GetCurrentUserId();

		var (positionsError, positions) = await GetActivePositionsAsync(userId, cancellationToken);
		if (positionsError != null)
			return positionsError;
		if (positions == null) // no holdings at all, not an error, just an empty forecast
			return ApiResponse.Create(new List<ForecastDayDto>(), System.Net.HttpStatusCode.OK);

		var (historyError, history) = await GetPriceHistoryAsync(positions.QuoteIds, cancellationToken);
		if (historyError != null)
			return historyError;

		int quoteCount = positions.QuoteIds.Length;
		var quoteIndex = new Dictionary<int, int>(quoteCount);
		for (int i = 0; i < quoteCount; i++)
			quoteIndex[positions.QuoteIds[i]] = i;

		double[] currentValues = new double[quoteCount];
		for (int q = 0; q < quoteCount; q++)
			currentValues[q] = (double)positions.CurrentValueByQuote[positions.QuoteIds[q]];

		var (groupOfQuote, uniqueGroupIds, groupCount) = BuildGroupMapping(positions.QuoteIds, positions.GroupByQuote);

		ContributionSchedule schedule = includeContributions
			? await GetContributionScheduleAsync(userId, positions.QuoteIds, quoteIndex, quoteCount, cancellationToken)
			: ContributionSchedule.Empty(quoteCount);

		SimulationResult simulation = RunSimulations(
			currentValues, history!.RatioMatrix, groupOfQuote, groupCount, quoteCount,
			schedule, includeContributions, cancellationToken);

		DateOnly[] forecastDates = GenerateForecastDates();

		List<ForecastDayDto> result = BuildForecastDtos(
			forecastDates, simulation, positions.QuoteIds, uniqueGroupIds, groupCount, quoteCount);

		return ApiResponse.Create(result, System.Net.HttpStatusCode.OK);
	}

	// ---------- Data loading ----------

	private record ActivePositions(int[] QuoteIds, Dictionary<int, decimal> CurrentValueByQuote, Dictionary<int, int?> GroupByQuote);

	private async Task<(ApiResponse<List<ForecastDayDto>>? Error, ActivePositions? Data)> GetActivePositionsAsync(Guid userId, CancellationToken cancellationToken)
	{
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

		var activePositions = investmentsByQuote.Where(kvp => kvp.Value > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		if (activePositions.Count == 0)
			return (null, null);

		int[] quoteIds = activePositions.Keys.OrderBy(id => id).ToArray();
		int quoteCount = quoteIds.Length;

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

		if (quotePriceAndGroupData.Any(q => q.LatestPrice == null || q.LatestPrice <= 0))
		{
			var error = ApiResponse.Create<List<ForecastDayDto>>(
				"Invalid data", "One or more quotes have invalid price data (null, zero, or negative)", System.Net.HttpStatusCode.BadRequest);
			return (error, null);
		}

		Dictionary<int, decimal> currentValueByQuote = new(quoteCount);
		Dictionary<int, int?> groupByQuote = new(quoteCount);

		foreach (var quoteData in quotePriceAndGroupData)
		{
			decimal totalAmount = activePositions[quoteData.Id];
			decimal latestPrice = quoteData.LatestPrice ?? 0;
			currentValueByQuote[quoteData.Id] = totalAmount * latestPrice;
			groupByQuote[quoteData.Id] = quoteData.GroupId;
		}

		return (null, new ActivePositions(quoteIds, currentValueByQuote, groupByQuote));
	}

	// ---------- Price history / return matrix ----------

	private record PriceHistoryData(DateTime[] DateOrder, double[][] RatioMatrix);

	private async Task<(ApiResponse<List<ForecastDayDto>>? Error, PriceHistoryData? Data)> GetPriceHistoryAsync(int[] quoteIds, CancellationToken cancellationToken)
	{
		int quoteCount = quoteIds.Length;

		var priceRows = await databaseContext.QuotePrices
			.AsNoTracking()
			.Where(p => quoteIds.Contains(p.QuoteId))
			.Select(p => new { p.QuoteId, p.Date, p.AdjustedClose })
			.ToListAsync(cancellationToken);

		DateTime[] dateOrder = priceRows
			.GroupBy(r => r.Date)
			.Where(g => g.Select(x => x.QuoteId).Distinct().Count() == quoteCount)
			.Select(g => g.Key)
			.OrderBy(d => d)
			.ToArray();

		int dateCount = dateOrder.Length;

		if (dateCount < 2)
		{
			var error = ApiResponse.Create<List<ForecastDayDto>>(
				"Insufficient data", "Not enough overlapping price history across the selected quotes to build a forecast.", System.Net.HttpStatusCode.BadRequest);
			return (error, null);
		}

		int returnCount = dateCount - 1;
		if (returnCount < blockLength)
		{
			var error = ApiResponse.Create<List<ForecastDayDto>>(
				"Insufficient data",
				$"Only {returnCount} days of overlapping return history available, but a block length of {blockLength} is required. Add more price history or reduce the block length.",
				System.Net.HttpStatusCode.BadRequest);
			return (error, null);
		}

		var dateIndex = new Dictionary<DateTime, int>(dateCount);
		for (int i = 0; i < dateCount; i++)
			dateIndex[dateOrder[i]] = i;

		var quoteIndex = new Dictionary<int, int>(quoteCount);
		for (int i = 0; i < quoteCount; i++)
			quoteIndex[quoteIds[i]] = i;

		double[][] priceMatrix = new double[dateCount][];
		for (int r = 0; r < dateCount; r++)
			priceMatrix[r] = new double[quoteCount];

		foreach (var row in priceRows)
		{
			if (!dateIndex.TryGetValue(row.Date, out int r))
				continue;

			priceMatrix[r][quoteIndex[row.QuoteId]] = (double)row.AdjustedClose;
		}

		for (int r = 0; r < dateCount; r++)
			for (int c = 0; c < quoteCount; c++)
				if (priceMatrix[r][c] <= 0)
					throw new InvalidOperationException(
						$"Invalid price {priceMatrix[r][c]} for quote {quoteIds[c]} on {dateOrder[r]:yyyy-MM-dd}");

		double[][] ratioMatrix = new double[returnCount][];
		for (int r = 0; r < returnCount; r++)
		{
			double[] rowRatios = new double[quoteCount];
			for (int c = 0; c < quoteCount; c++)
				rowRatios[c] = priceMatrix[r + 1][c] / priceMatrix[r][c];
			ratioMatrix[r] = rowRatios;
		}

		return (null, new PriceHistoryData(dateOrder, ratioMatrix));
	}

	// ---------- Group mapping ----------

	private static (int[] GroupOfQuote, int?[] UniqueGroupIds, int GroupCount) BuildGroupMapping(int[] quoteIds, Dictionary<int, int?> groupByQuote)
	{
		int?[] uniqueGroupIds = groupByQuote.Values.Distinct().ToArray();
		int groupCount = uniqueGroupIds.Length;

		var groupSlot = new Dictionary<int?, int>(groupCount);
		for (int i = 0; i < groupCount; i++)
			groupSlot[uniqueGroupIds[i]] = i;

		int[] groupOfQuote = new int[quoteIds.Length];
		for (int q = 0; q < quoteIds.Length; q++)
			groupOfQuote[q] = groupSlot[groupByQuote.GetValueOrDefault(quoteIds[q])];

		return (groupOfQuote, uniqueGroupIds, groupCount);
	}

	// ---------- Contribution schedule ----------

	private readonly record struct ContributionSchedule(int[] Interval, int[] FirstDay, double[] Amount)
	{
		public static ContributionSchedule Empty(int quoteCount) =>
			new(new int[quoteCount], new int[quoteCount], new double[quoteCount]);
	}

	private async Task<ContributionSchedule> GetContributionScheduleAsync(
		Guid userId, int[] quoteIds, Dictionary<int, int> quoteIndex, int quoteCount, CancellationToken cancellationToken)
	{
		ContributionSchedule schedule = ContributionSchedule.Empty(quoteCount);

		var buyHistory = await databaseContext.Investments
			.AsNoTracking()
			.Where(i => i.UserId == userId && i.Type == InvestmentType.Buy && !i.ExcludeFromForecast && quoteIds.Contains(i.QuoteId))
			.OrderBy(i => i.Date)
			.Select(i => new { i.QuoteId, i.Date, i.Amount, i.PricePerUnit })
			.ToListAsync(cancellationToken);

		DateTime today = DateTime.UtcNow;

		foreach (var group in buyHistory.GroupBy(b => b.QuoteId))
		{
			var purchases = group.OrderBy(b => b.Date).ToList();
			if (purchases.Count < 2)
				continue; // no cadence derivable from a single purchase

			List<double> gapsInDays = new(purchases.Count - 1);
			for (int i = 1; i < purchases.Count; i++)
				gapsInDays.Add((purchases[i].Date - purchases[i - 1].Date).TotalDays);

			double avgGapDays = gapsInDays.Average();
			double avgCashPerPurchase = purchases.Average(p => (double)(p.Amount * p.PricePerUnit));

			int intervalTradingDays = Math.Max(1, (int)Math.Round(avgGapDays * tradingDaysPerCalendarDay));

			double daysSinceLastPurchase = (today - purchases[^1].Date).TotalDays;
			int daysSinceLastPurchaseTradingDays = (int)Math.Round(daysSinceLastPurchase * tradingDaysPerCalendarDay);

			int offset = Math.Max(0, intervalTradingDays - daysSinceLastPurchaseTradingDays);

			int q = quoteIndex[group.Key];
			schedule.Interval[q] = intervalTradingDays;
			schedule.FirstDay[q] = offset;
			schedule.Amount[q] = avgCashPerPurchase;
		}

		return schedule;
	}

	// ---------- Simulation ----------

	private record SimulationResult(double[] PortfolioSeries, double[][] GroupSeries, double[][] QuoteSeries);

	private static SimulationResult RunSimulations(
		double[] currentValues, double[][] ratioMatrix, int[] groupOfQuote, int groupCount, int quoteCount,
		ContributionSchedule schedule, bool includeContributions, CancellationToken cancellationToken)
	{
		int returnLen = ratioMatrix.Length;
		int cellCount = horizon * simulationCount;

		double[] portfolioSeries = new double[cellCount];
		double[][] groupSeries = new double[groupCount][];
		for (int g = 0; g < groupCount; g++) groupSeries[g] = new double[cellCount];
		double[][] quoteSeries = new double[quoteCount][];
		for (int q = 0; q < quoteCount; q++) quoteSeries[q] = new double[cellCount];

		var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken };

		Parallel.For(
			0, simulationCount, parallelOptions,
			() => (rng: new Random(), running: new double[quoteCount], groupSums: new double[groupCount]),
			(sim, _, local) =>
			{
				var (rng, running, groupSums) = local;
				Array.Copy(currentValues, running, quoteCount);

				int blockPos = blockLength;
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
						running[q] *= dayRatios[q];

						if (includeContributions &&
							schedule.Interval[q] > 0 &&
							day >= schedule.FirstDay[q] &&
							(day - schedule.FirstDay[q]) % schedule.Interval[q] == 0)
						{
							running[q] += schedule.Amount[q];
						}

						double v = running[q];
						quoteSeries[q][idx] = v;
						portfolioSum += v;
						groupSums[groupOfQuote[q]] += v;
					}

					portfolioSeries[idx] = portfolioSum;
					for (int g = 0; g < groupCount; g++)
						groupSeries[g][idx] = groupSums[g];
				}

				return local;
			},
			_ => { });

		return new SimulationResult(portfolioSeries, groupSeries, quoteSeries);
	}

	// ---------- Dates & DTO assembly ----------

	private static DateOnly[] GenerateForecastDates()
	{
		DateOnly[] forecastDates = new DateOnly[horizon];
		DateOnly current = DateOnly.FromDateTime(DateTime.UtcNow);

		for (int i = 0; i < horizon; i++)
		{
			current = current.AddDays(1);
			while (current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
				current = current.AddDays(1);
			forecastDates[i] = current;
		}

		return forecastDates;
	}

	private static List<ForecastDayDto> BuildForecastDtos(
		DateOnly[] forecastDates, SimulationResult simulation, int[] quoteIds, int?[] uniqueGroupIds, int groupCount, int quoteCount)
	{
		ForecastBand[] portfolioBands = ExtractBands(simulation.PortfolioSeries, horizon, simulationCount);

		ForecastBand[][] groupBands = new ForecastBand[groupCount][];
		for (int g = 0; g < groupCount; g++)
			groupBands[g] = ExtractBands(simulation.GroupSeries[g], horizon, simulationCount);

		ForecastBand[][] quoteBands = new ForecastBand[quoteCount][];
		for (int q = 0; q < quoteCount; q++)
			quoteBands[q] = ExtractBands(simulation.QuoteSeries[q], horizon, simulationCount);

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

		return result;
	}

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