using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Investment;

public class InvestmentManagement(DatabaseContext databaseContext)
{
    public async Task<IEnumerable<InvestmentModel>> GetInvestmentsAsync()
    {
        // TODO: Filter investments by user ID
        return await databaseContext.Investments.ToListAsync();
    }

    public async Task<InvestmentModel?> GetInvestmentByIdAsync(int id)
    {
        return await databaseContext.Investments.FindAsync(id);
    }

    public async Task<InvestmentModel> CreateInvestmentAsync(InvestmentRequest investmentRequest)
    {
        if (investmentRequest is null)
            throw new ArgumentNullException(nameof(investmentRequest));

        InvestmentModel investment = new InvestmentModel
        {
            ProviderId = investmentRequest.ProviderId,
            QuoteSymbol = investmentRequest.QuoteSymbol,
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

    public async Task<InvestmentModel> UpdateInvestmentAsync(int id, InvestmentRequest investmentRequest)
    {
        if (investmentRequest is null)
            throw new ArgumentNullException(nameof(investmentRequest));

        InvestmentModel? investment = await databaseContext.Investments.FindAsync(id);
        
        if (investment is null)
            throw new KeyNotFoundException($"Investment with ID {id} not found.");

        investment.ProviderId = investmentRequest.ProviderId;
        investment.QuoteSymbol = investmentRequest.QuoteSymbol;
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

        databaseContext.Investments.Remove(investment);
        await databaseContext.SaveChangesAsync();
    }
}