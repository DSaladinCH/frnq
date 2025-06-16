using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Position;

public class PositionManagement(DatabaseContext databaseContext)
{
    public async Task<IEnumerable<PositionSnapshot>> GetPositionsAsync(DateTime from, DateTime to)
    {
        from = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        to = DateTime.SpecifyKind(to, DateTimeKind.Utc);

        // Get all investments up to the requested 'to' date
        List<InvestmentModel> investments = await databaseContext.Investments
            .Include(i => i.Quote)
            .Where(i => i.Date <= to)
            .OrderBy(i => i.Date)
            .ToListAsync();

        // Get all prices up to the requested 'to' date
        List<QuotePrice> prices = await databaseContext.QuotePrices
            .Where(qp => qp.Date <= to)
            .ToListAsync();

        Dictionary<(string ProviderId, string Symbol), Dictionary<DateTime, QuotePrice>> priceLookup = prices
            .GroupBy(qp => (qp.ProviderId, qp.Symbol))
            .ToDictionary(g => g.Key, g => g.ToDictionary(p => p.Date.Date, p => p));

        // Find the earliest investment date
        DateTime? firstInvestmentDate = investments.Count > 0 ? investments.Min(i => i.Date.Date) : (DateTime?)null;
        if (firstInvestmentDate == null)
            return [];

        List<DateTime> days = Enumerable.Range(0, (to.Date - firstInvestmentDate.Value).Days + 1)
            .Select(offset => firstInvestmentDate.Value.AddDays(offset))
            .ToList();

        List<PositionSnapshot> allSnapshots = [];

        var investmentGroups = investments.GroupBy(i => (i.UserId, i.ProviderId, i.QuoteSymbol));

        foreach (var group in investmentGroups)
        {
            decimal cumulativeAmount = 0;
            decimal weightedPriceSum = 0;
            decimal totalFees = 0;
            decimal realizedGain = 0;
            Queue<(decimal Amount, decimal PricePerUnit)> lots = new();
            int invIndex = 0;
            List<InvestmentModel> invs = group.OrderBy(i => i.Date).ToList();
            QuoteModel quote = invs.First().Quote;
            QuotePrice? lastKnownPrice = null;

            for (int d = 0; d < days.Count; d++)
            {
                DateTime day = days[d];

                // Apply all investments on this day
                while (invIndex < invs.Count && invs[invIndex].Date.Date == day)
                {
                    InvestmentModel inv = invs[invIndex];

                    if (inv.Type == InvestmentType.Buy)
                    {
                        cumulativeAmount += inv.Amount;
                        weightedPriceSum += inv.Amount * inv.PricePerUnit;
                        totalFees += inv.TotalFees;
                        lots.Enqueue((inv.Amount, inv.PricePerUnit));
                    }
                    else if (inv.Type == InvestmentType.Sell)
                    {
                        decimal sellAmount = -inv.Amount;
                        decimal sellProceeds = sellAmount * inv.PricePerUnit - inv.TotalFees;
                        decimal costBasis = 0;
                        decimal toSell = sellAmount;

                        while (toSell > 0 && lots.Count > 0)
                        {
                            (decimal Amount, decimal PricePerUnit) lot = lots.Peek();
                            decimal used = Math.Min(lot.Amount, toSell);
                            costBasis += used * lot.PricePerUnit;

                            if (used == lot.Amount)
                                lots.Dequeue();
                            else
                                lots = new Queue<(decimal, decimal)>(lots.Select(l => l == lot ? (l.Amount - used, l.PricePerUnit) : l));
                            toSell -= used;
                        }

                        realizedGain += sellProceeds - costBasis;
                        cumulativeAmount += inv.Amount;
                        weightedPriceSum += inv.Amount * inv.PricePerUnit;
                        totalFees += inv.TotalFees;
                    }
                    else if (inv.Type == InvestmentType.Dividend)
                    {
                        // For dividends, add the amount to realized gain only
                        realizedGain += inv.Amount;
                    }
                    invIndex++;
                }

                // Get price for this day
                QuotePrice? price = null;
                if (priceLookup.TryGetValue((group.Key.ProviderId, group.Key.QuoteSymbol), out var priceDict))
                    priceDict.TryGetValue(day, out price);

                if (price == null && lastKnownPrice == null)
                    continue;

                if (price == null)
                    price = lastKnownPrice;
                else
                    lastKnownPrice = price;

                decimal avgPrice = cumulativeAmount != 0 ? weightedPriceSum / cumulativeAmount : 0m;
                decimal marketPrice = (price?.AdjustedClose ?? price?.Close) ?? 0m;
                decimal totalValue = marketPrice * cumulativeAmount;
                decimal invested = 0;
                
                foreach (var lot in lots)
                    invested += lot.Amount * lot.PricePerUnit;

                decimal unrealizedGain = totalValue - invested;

                allSnapshots.Add(new PositionSnapshot
                {
                    UserId = group.Key.UserId,
                    ProviderId = group.Key.ProviderId,
                    QuoteSymbol = group.Key.QuoteSymbol,
                    Date = day,
                    Currency = quote.Currency,
                    Amount = cumulativeAmount,
                    Invested = invested + totalFees,
                    MarketPricePerUnit = marketPrice,
                    TotalFees = totalFees,
                    RealizedGain = realizedGain,
                    UnrealizedGain = unrealizedGain
                });
            }
        }
        
        // Only return snapshots within the requested range
        return allSnapshots.Where(s => s.Date >= from.Date && s.Date <= to.Date).ToList();
    }
}