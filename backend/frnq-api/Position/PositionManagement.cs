using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Position;

public class PositionManagement(DatabaseContext databaseContext)
{
    public async Task<IEnumerable<PositionSnapshot>> GetPositionsAsync(DateTime from, DateTime to)
    {
        List<InvestmentModel> investments = await databaseContext.Investments
            .Include(i => i.Quote)
            .OrderBy(i => i.Date)
            .ToListAsync();

        List<QuotePrice> prices = await databaseContext.QuotePrices
            .Where(qp => qp.Date >= from && qp.Date <= to)
            .ToListAsync();

        // Will handle prices by day -> Maybe change later to handle intraday prices
        Dictionary<(string ProviderId, string Symbol), Dictionary<DateTime, QuotePrice>> priceLookup = prices
            .GroupBy(qp => (qp.ProviderId, qp.Symbol))
            .ToDictionary(g => g.Key, g => g.ToDictionary(p => p.Date.Date, p => p));

        List<DateTime> days = Enumerable.Range(0, (to - from).Days + 1)
            .Select(offset => from.Date.AddDays(offset))
            .ToList();

        List<PositionSnapshot> snapshots = [];

        // Group investments by (UserId, ProviderId, QuoteSymbol)
        IEnumerable<IGrouping<(string UserId, string ProviderId, string QuoteSymbol), InvestmentModel>> investmentGroups = investments
            .GroupBy(i => (i.UserId, i.ProviderId, i.QuoteSymbol));

        foreach (IGrouping<(string UserId, string ProviderId, string QuoteSymbol), InvestmentModel> group in investmentGroups)
        {
            decimal cumulativeAmount = 0;
            decimal weightedPriceSum = 0;
            decimal totalFees = 0;
            int invIndex = 0;
            List<InvestmentModel> invs = group.OrderBy(i => i.Date).ToList();
            QuoteModel quote = invs.First().Quote;

            for (int d = 0; d < days.Count; d++)
            {
                DateTime day = days[d];

                // Apply all investments on this day
                while (invIndex < invs.Count && invs[invIndex].Date.Date == day)
                {
                    cumulativeAmount += invs[invIndex].Amount;
                    weightedPriceSum += invs[invIndex].Amount * invs[invIndex].PricePerUnit;
                    totalFees += invs[invIndex].TotalFees;
                    invIndex++;
                }

                if (cumulativeAmount == 0)
                    continue; // No position on this day

                // Get price for this day
                QuotePrice? price = null;

                if (priceLookup.TryGetValue((group.Key.ProviderId, group.Key.QuoteSymbol), out var priceDict))
                    priceDict.TryGetValue(day, out price);

                if (price == null)
                    continue; // No price for this day

                decimal avgPrice = weightedPriceSum / cumulativeAmount;

                snapshots.Add(new PositionSnapshot
                {
                    UserId = group.Key.UserId,
                    ProviderId = group.Key.ProviderId,
                    QuoteSymbol = group.Key.QuoteSymbol,
                    Date = day,
                    Currency = quote.Currency,
                    Amount = cumulativeAmount,
                    PricePerUnit = avgPrice,
                    MarketPricePerUnit = price.AdjustedClose ?? price.Close,
                    TotalFees = totalFees,
                    Quote = quote
                });
            }
        }
        return snapshots;
    }
}