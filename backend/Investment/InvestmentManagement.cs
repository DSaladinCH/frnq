using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Investment;

public class InvestmentManagement(QuoteManagement quoteManagement, DatabaseContext databaseContext, AuthManagement authManagement)
{
	private readonly Guid userId = authManagement.GetCurrentUserId();

	public async Task<ApiResponse<PaginatedInvestmentsResponse>> GetInvestmentsAsync(
		int skip = 0,
		int take = 50,
		DateTime? fromDate = null,
		DateTime? toDate = null,
		int? quoteId = null,
		int? groupId = null,
		InvestmentType? type = null)
	{
		IQueryable<InvestmentModel> query = databaseContext.Investments.Where(i => i.UserId == userId);

		// Apply filters
		if (fromDate.HasValue)
		{
			DateTime fromDateUtc = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
			query = query.Where(i => i.Date >= fromDateUtc);
		}

		if (toDate.HasValue)
		{
			DateTime toDateUtc = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
			query = query.Where(i => i.Date <= toDateUtc);
		}

		if (quoteId.HasValue)
			query = query.Where(i => i.QuoteId == quoteId.Value);

		if (groupId.HasValue)
		{
			// Filter by group - join with QuoteGroupMapping
			IQueryable<int> quoteIdsInGroup = databaseContext.QuoteGroupMappings
				.Where(m => m.UserId == userId && m.GroupId == groupId.Value)
				.Select(m => m.QuoteId);

			query = query.Where(i => quoteIdsInGroup.Contains(i.QuoteId));
		}

		if (type.HasValue)
			query = query.Where(i => i.Type == type.Value);

		int totalCount = await query.CountAsync();
		List<InvestmentModel> investments = await query
			.OrderByDescending(i => i.Date)
			.Skip(skip)
			.Take(take)
			.ToListAsync();

		return ApiResponse.Create(new PaginatedInvestmentsResponse
		{
			Items = investments.Select(i => new InvestmentViewDto(i)).ToList(),
			TotalCount = totalCount,
			Skip = skip,
			Take = take
		}, System.Net.HttpStatusCode.OK);
	}

	public async Task<ApiResponse<InvestmentViewDto>> GetInvestmentByIdAsync(int id)
	{
		InvestmentModel? investment = await databaseContext.Investments.Where(i => i.UserId == userId && i.Id == id).FirstOrDefaultAsync();

		if (investment is null)
			return ApiResponses.NotFound404;

		return ApiResponse.Create(new InvestmentViewDto(investment), System.Net.HttpStatusCode.OK);
	}

	public async Task<ApiResponse> CreateInvestmentAsync(InvestmentDto investmentRequest)
	{
		QuoteModel? quote = await quoteManagement.GetQuoteAsync(investmentRequest);

		if (quote is null)
		{
			await quoteManagement.GetHistoricalPricesAsync(investmentRequest.ProviderId, investmentRequest.QuoteSymbol, DateTime.MinValue, DateTime.UtcNow);
			quote = await quoteManagement.GetQuoteAsync(investmentRequest);
		}

		if (quote is null)
			return ApiResponses.NotFound404;

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

		return ApiResponses.Created201;
	}

	public async Task<ApiResponse<List<InvestmentViewDto>>> CreateInvestmentsAsync(List<InvestmentDto> investmentRequests)
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
				return ApiResponses.NotFound404;

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

		return ApiResponse.Create(investments.Select(i => new InvestmentViewDto(i)).ToList(), System.Net.HttpStatusCode.Created);
	}

	public async Task<ApiResponse<InvestmentViewDto>> UpdateInvestmentAsync(int id, InvestmentDto investmentRequest)
	{
		InvestmentModel? investment = await databaseContext.Investments.FindAsync(id);

		if (investment is null)
			return ApiResponses.NotFound404;

		if (investment.UserId != userId)
			return ApiResponses.Unauthorized401;

		QuoteModel? quote = await quoteManagement.GetQuoteAsync(investmentRequest);

		if (quote is null)
		{
			await quoteManagement.GetHistoricalPricesAsync(investmentRequest.ProviderId, investmentRequest.QuoteSymbol, DateTime.MinValue, DateTime.UtcNow);
			quote = await quoteManagement.GetQuoteAsync(investmentRequest);
		}

		if (quote is null)
			return ApiResponses.NotFound404;

		investment.QuoteId = quote.Id;
		investment.Date = DateTime.SpecifyKind(investmentRequest.Date, DateTimeKind.Utc);
		investment.Type = investmentRequest.Type;
		investment.Amount = investmentRequest.Amount;
		investment.PricePerUnit = investmentRequest.PricePerUnit;
		investment.TotalFees = investmentRequest.TotalFees;

		databaseContext.Investments.Update(investment);
		await databaseContext.SaveChangesAsync();

		return ApiResponse.Create(new InvestmentViewDto(investment), System.Net.HttpStatusCode.OK);
	}

	public async Task<ApiResponse> DeleteInvestmentAsync(int id)
	{
		InvestmentModel? investment = await databaseContext.Investments.Where(i => i.UserId == userId && i.Id == id).FirstOrDefaultAsync();

		if (investment is null)
			return ApiResponses.NotFound404;

		databaseContext.Investments.Remove(investment);
		await databaseContext.SaveChangesAsync();

		return ApiResponses.NoContent204;
	}
}