using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Position;

public class PositionManagement(DatabaseContext databaseContext, IServiceProvider serviceProvider, AuthManagement authManagement)
{
    private readonly Guid userId = authManagement.GetCurrentUserId();

    public async Task<PositionsResponse> GetPositionsAsync(DateTime? from, DateTime? to)
    {
        from ??= DateTime.MinValue;
        to ??= DateTime.UtcNow;

        from = DateTime.SpecifyKind((DateTime)from, DateTimeKind.Utc);
        to = DateTime.SpecifyKind((DateTime)to, DateTimeKind.Utc);

        // Get all investments up to the requested 'to' date
        List<InvestmentModel> investments = await databaseContext.Investments
            .Where(i => userId == i.UserId && i.Date <= to)
            .OrderBy(i => i.Date)
            .Include(i => i.Quote)
            .ThenInclude(q => q.Mappings.Where(m => m.UserId == userId))
                .ThenInclude(m => m.Group)
            .ToListAsync();

        // Get all quotes that have been invested in
        if (investments.Count == 0)
            return new PositionsResponse { Snapshots = [], Quotes = [] };

        HashSet<int> quoteIds = investments.Select(i => i.QuoteId).ToHashSet();

        // Update historical prices with any missing data
        await Parallel.ForEachAsync(quoteIds, new ParallelOptions() { MaxDegreeOfParallelism = 2 }, async (quoteId, ct) =>
        {
            // Create a new scope for each parallel operation
            using IServiceScope scope = serviceProvider.CreateScope();
            QuoteManagement scopedQuoteManagement = scope.ServiceProvider.GetRequiredService<QuoteManagement>();

            await scopedQuoteManagement.GetHistoricalPricesAsync(quoteId, (DateTime)from, (DateTime)to);
        });

        // Get all prices up to the requested 'to' date
        List<QuotePrice> prices = await databaseContext.QuotePrices
            .Where(qp => quoteIds.Contains(qp.QuoteId))
            .Where(qp => qp.Date <= to)
            .ToListAsync();

        Dictionary<int, Dictionary<DateTime, QuotePrice>> priceLookup = prices
            .GroupBy(qp => qp.QuoteId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(p => p.Date.Date, p => p));

        // Find the earliest investment date
        DateTime? firstInvestmentDate = investments.Count > 0 ? investments.Min(i => i.Date.Date) : (DateTime?)null;

        if (firstInvestmentDate == null)
            return new PositionsResponse { Snapshots = [], Quotes = [] };

        List<DateTime> days = [.. Enumerable.Range(0, (((DateTime)to).Date
            - firstInvestmentDate.Value).Days + 1).Select(offset => firstInvestmentDate.Value.AddDays(offset))];

        List<PositionSnapshot> allSnapshots = [];
        IEnumerable<IGrouping<(Guid UserId, int QuoteId), InvestmentModel>> investmentGroups =
    investments.GroupBy(i => (i.UserId, i.QuoteId));

        foreach (var group in investmentGroups)
        {
            decimal totalFees = 0;           // lifetime fees for info
            decimal realizedCash = 0;        // dividends + net sell proceeds
            decimal investedCash = 0;        // buys – net sell proceeds
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
                        decimal totalCost = inv.Amount * inv.PricePerUnit + inv.TotalFees;
                        investedCash += totalCost;
                        totalFees += inv.TotalFees;

                        // track cost basis for remaining shares
                        decimal effectivePrice = inv.PricePerUnit + (inv.TotalFees / inv.Amount);
                        lots.Enqueue((inv.Amount, effectivePrice));
                    }
                    else if (inv.Type == InvestmentType.Sell)
                    {
                        decimal sellAmount = inv.Amount;
                        decimal grossProceeds = sellAmount * inv.PricePerUnit;
                        decimal netProceeds = grossProceeds - inv.TotalFees;

                        realizedCash += netProceeds;
                        investedCash -= netProceeds;   // money you got back reduces "still invested"
                        totalFees += inv.TotalFees;

                        // update remaining shares (FIFO)
                        decimal toSell = sellAmount;
                        while (toSell > 0 && lots.Count > 0)
                        {
                            var lot = lots.Peek();
                            decimal used = Math.Min(lot.Amount, toSell);

                            if (used == lot.Amount)
                            {
                                lots.Dequeue();
                            }
                            else
                            {
                                lots.Dequeue();
                                lots = new Queue<(decimal, decimal)>(
                                    new[] { (lot.Amount - used, lot.PricePerUnit) }
                                    .Concat(lots)
                                );
                            }

                            toSell -= used;
                        }
                    }
                    else if (inv.Type == InvestmentType.Dividend)
                    {
                        realizedCash += inv.Amount;  // pure cash in
                    }

                    invIndex++;
                }

                // Get price for this day
                QuotePrice? price = null;
                if (priceLookup.TryGetValue(group.Key.QuoteId, out var priceDict))
                    priceDict.TryGetValue(day, out price);

                if (price == null && lastKnownPrice == null)
                    continue;

                if (price == null)
                    price = lastKnownPrice;
                else
                    lastKnownPrice = price;

                decimal marketPrice = (price?.AdjustedClose ?? price?.Close) ?? 0m;
                decimal currentShares = lots.Sum(l => l.Amount);
                decimal marketValue = marketPrice * currentShares;

                // total value = remaining shares value + realized cash (dividends + sells)
                decimal totalValue = marketValue + realizedCash;
                decimal profit = totalValue - investedCash;

                allSnapshots.Add(new PositionSnapshot
                {
                    UserId = group.Key.UserId.ToString(),
                    QuoteId = group.Key.QuoteId,
                    Date = day,
                    Currency = quote.Currency,
                    Amount = currentShares,
                    Invested = investedCash,         // total money you still have tied up
                    TotalFees = totalFees,
                    MarketPricePerUnit = marketPrice,
                    RealizedGain = realizedCash,     // NEW: total dividends + net sells
                });
            }
        }

        // Only return snapshots within the requested range
        List<PositionSnapshot> filteredSnapshots =
            allSnapshots.Where(s => s.Date >= ((DateTime)from).Date && s.Date <= ((DateTime)to).Date).ToList();

        // Get unique quotes used in the investments
        List<QuoteModel> uniqueQuotes = [.. investments
            .Select(i => i.Quote)
            .Where(q => q != null)
            .GroupBy(q => q.Id)
            .Select(g => g.First())];

        return new PositionsResponse
        {
            Snapshots = filteredSnapshots,
            Quotes = [.. uniqueQuotes.Select(QuoteDto.FromModel)]
        };
    }
}