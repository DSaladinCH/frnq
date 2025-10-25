using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Quote;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Investment;

public class InvestmentManagement(QuoteManagement quoteManagement, DatabaseContext databaseContext, AuthManagement authManagement)
{
    private readonly Guid userId = authManagement.GetCurrentUserId();
    
    public async Task<PaginatedInvestmentsResponse> GetInvestmentsAsync(
        int skip = 0, 
        int take = 50,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? quoteId = null,
        int? groupId = null,
        InvestmentType? type = null)
    {
        var query = databaseContext.Investments.Where(i => i.UserId == userId);
        
        // Apply filters
        if (fromDate.HasValue)
        {
            var fromDateUtc = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(i => i.Date >= fromDateUtc);
        }
        
        if (toDate.HasValue)
        {
            var toDateUtc = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(i => i.Date <= toDateUtc);
        }
        
        if (quoteId.HasValue)
        {
            query = query.Where(i => i.QuoteId == quoteId.Value);
        }
        
        if (groupId.HasValue)
        {
            // Filter by group - join with QuoteGroupMapping
            var quoteIdsInGroup = databaseContext.QuoteGroupMappings
                .Where(m => m.UserId == userId && m.GroupId == groupId.Value)
                .Select(m => m.QuoteId);
            
            query = query.Where(i => quoteIdsInGroup.Contains(i.QuoteId));
        }
        
        if (type.HasValue)
        {
            query = query.Where(i => i.Type == type.Value);
        }
        
        var totalCount = await query.CountAsync();
        var investments = await query
            .OrderByDescending(i => i.Date)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        
        return new PaginatedInvestmentsResponse
        {
            Items = investments,
            TotalCount = totalCount,
            Skip = skip,
            Take = take
        };
    }

    public async Task<InvestmentModel?> GetInvestmentByIdAsync(int id)
    {
        return await databaseContext.Investments.Where(i => i.UserId == userId && i.Id == id).FirstOrDefaultAsync();
    }

    public async Task<InvestmentModel> CreateInvestmentAsync(InvestmentRequest investmentRequest)
    {
        QuoteModel? quote = await quoteManagement.GetQuoteAsync(investmentRequest);

        if (quote is null)
        {
            await quoteManagement.GetHistoricalPricesAsync(investmentRequest.ProviderId, investmentRequest.QuoteSymbol, DateTime.MinValue, DateTime.UtcNow);
            quote = await quoteManagement.GetQuoteAsync(investmentRequest);
        }

        if (quote is null)
            throw new ArgumentException($"Referenced quote does not exist.");

        InvestmentModel investment = new()
        {
            UserId = userId,
            QuoteId = quote.Id,
            Date = DateTime.SpecifyKind(investmentRequest.Date, DateTimeKind.Utc),
            Type = investmentRequest.Type,
            Amount = investmentRequest.Amount,
            PricePerUnit = investmentRequest.PricePerUnit,
            TotalFees = investmentRequest.TotalFees
        };

        databaseContext.Investments.Add(investment);
        await databaseContext.SaveChangesAsync();

        return investment;
    }

    public async Task<List<InvestmentModel>> CreateInvestmentsAsync(List<InvestmentRequest> investmentRequests)
    {
        var investments = new List<InvestmentModel>();
        
        foreach (var investmentRequest in investmentRequests)
        {
            QuoteModel? quote = await quoteManagement.GetQuoteAsync(investmentRequest);

            if (quote is null)
            {
                await quoteManagement.GetHistoricalPricesAsync(investmentRequest.ProviderId, investmentRequest.QuoteSymbol, DateTime.MinValue, DateTime.UtcNow);
                quote = await quoteManagement.GetQuoteAsync(investmentRequest);
            }

            if (quote is null)
                throw new ArgumentException($"Referenced quote does not exist for symbol {investmentRequest.QuoteSymbol}.");

            InvestmentModel investment = new()
            {
                UserId = userId,
                QuoteId = quote.Id,
                Date = DateTime.SpecifyKind(investmentRequest.Date, DateTimeKind.Utc),
                Type = investmentRequest.Type,
                Amount = investmentRequest.Amount,
                PricePerUnit = investmentRequest.PricePerUnit,
                TotalFees = investmentRequest.TotalFees
            };

            investments.Add(investment);
        }

        databaseContext.Investments.AddRange(investments);
        await databaseContext.SaveChangesAsync();

        return investments;
    }

    public async Task<InvestmentModel> UpdateInvestmentAsync(int id, InvestmentRequest investmentRequest)
    {
        if (investmentRequest is null)
            throw new ArgumentNullException(nameof(investmentRequest));

        InvestmentModel? investment = await databaseContext.Investments.FindAsync(id);

        if (investment is null)
            throw new KeyNotFoundException($"Investment with ID {id} not found.");

        if (investment.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to update this investment.");

        QuoteModel? quote = await quoteManagement.GetQuoteAsync(investmentRequest);

        if (quote is null)
        {
            await quoteManagement.GetHistoricalPricesAsync(investmentRequest.ProviderId, investmentRequest.QuoteSymbol, DateTime.MinValue, DateTime.UtcNow);
            quote = await quoteManagement.GetQuoteAsync(investmentRequest);
        }

        if (quote is null)
            throw new ArgumentException($"Referenced quote does not exist.");

        investment.QuoteId = quote.Id;
        investment.Date = DateTime.SpecifyKind(investmentRequest.Date, DateTimeKind.Utc);
        investment.Type = investmentRequest.Type;
        investment.Amount = investmentRequest.Amount;
        investment.PricePerUnit = investmentRequest.PricePerUnit;
        investment.TotalFees = investmentRequest.TotalFees;

        databaseContext.Investments.Update(investment);
        await databaseContext.SaveChangesAsync();

        return investment;
    }

    public async Task DeleteInvestmentAsync(int id)
    {
        InvestmentModel? investment = await GetInvestmentByIdAsync(id);

        if (investment is null)
            throw new KeyNotFoundException($"Investment with ID {id} not found.");

        if (investment.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to delete this investment.");

        databaseContext.Investments.Remove(investment);
        await databaseContext.SaveChangesAsync();
    }
}