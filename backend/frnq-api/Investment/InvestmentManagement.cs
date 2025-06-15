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

    public async Task<InvestmentModel> CreateInvestmentAsync(InvestmentModel investment)
    {
        if (investment is null)
            throw new ArgumentNullException(nameof(investment));

        databaseContext.Investments.Add(investment);
        await databaseContext.SaveChangesAsync();

        return investment;
    }

    public async Task<InvestmentModel> UpdateInvestmentAsync(InvestmentModel investment)
    {
        if (investment is null)
            throw new ArgumentNullException(nameof(investment));

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