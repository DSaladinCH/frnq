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
		InvestmentType? type = null,
		CancellationToken cancellationToken = default)
	{
		IQueryable<InvestmentModel> query = databaseContext.Investments
			.AsNoTracking()
			.Where(i => i.UserId == userId);

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

		int totalCount = await query.CountAsync(cancellationToken);
		List<InvestmentModel> investments = await query
			.OrderByDescending(i => i.Date)
			.Skip(skip)
			.Take(take)
			.ToListAsync(cancellationToken);

		return ApiResponse.Create(new PaginatedInvestmentsResponse
		{
			Items = investments.Select(i => new InvestmentViewDto(i)).ToList(),
			TotalCount = totalCount,
			Skip = skip,
			Take = take
		}, System.Net.HttpStatusCode.OK);
	}

	public async Task<ApiResponse<InvestmentViewDto>> GetInvestmentByIdAsync(int id, CancellationToken cancellationToken)
	{
		InvestmentModel? investment = await databaseContext.Investments
			.AsNoTracking()
			.Where(i => i.UserId == userId && i.Id == id).FirstOrDefaultAsync(cancellationToken);

		if (investment is null)
			return ApiResponses.NotFound404;

		return ApiResponse.Create(new InvestmentViewDto(investment), System.Net.HttpStatusCode.OK);
	}

	public async Task<ApiResponse> CreateInvestmentAsync(InvestmentDto investmentRequest, CancellationToken cancellationToken)
	{
		QuoteModel? quote = await quoteManagement.GetQuoteAsync(investmentRequest, cancellationToken);

		if (quote is null)
		{
			await quoteManagement.GetHistoricalPricesAsync(investmentRequest.ProviderId, investmentRequest.QuoteSymbol, DateTime.MinValue, DateTime.UtcNow, cancellationToken);
			quote = await quoteManagement.GetQuoteAsync(investmentRequest, cancellationToken);
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

		await databaseContext.Investments.AddAsync(investment, cancellationToken);
		await databaseContext.SaveChangesAsync(cancellationToken);

		return ApiResponses.Created201;
	}

	public async Task<ApiResponse<List<InvestmentViewDto>>> CreateInvestmentsAsync(List<InvestmentDto> investmentRequests, CancellationToken cancellationToken)
	{
		var investments = new List<InvestmentModel>();

		foreach (InvestmentDto investmentRequest in investmentRequests)
		{
			QuoteModel? quote = await quoteManagement.GetQuoteAsync(investmentRequest, cancellationToken);

			if (quote is null)
			{
				await quoteManagement.GetHistoricalPricesAsync(investmentRequest.ProviderId, investmentRequest.QuoteSymbol, DateTime.MinValue, DateTime.UtcNow, cancellationToken);
				quote = await quoteManagement.GetQuoteAsync(investmentRequest, cancellationToken);
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

		await databaseContext.Investments.AddRangeAsync(investments, cancellationToken);
		await databaseContext.SaveChangesAsync(cancellationToken);

		return ApiResponse.Create(investments.Select(i => new InvestmentViewDto(i)).ToList(), System.Net.HttpStatusCode.Created);
	}

	public async Task<ApiResponse<InvestmentViewDto>> UpdateInvestmentAsync(int id, InvestmentDto investmentRequest, CancellationToken cancellationToken)
	{
		InvestmentModel? investment = await databaseContext.Investments.FindAsync([id], cancellationToken);

		if (investment is null)
			return ApiResponses.NotFound404;

		if (investment.UserId != userId)
			return ApiResponses.Unauthorized401;

		QuoteModel? quote = await quoteManagement.GetQuoteAsync(investmentRequest, cancellationToken);

		if (quote is null)
		{
			await quoteManagement.GetHistoricalPricesAsync(investmentRequest.ProviderId, investmentRequest.QuoteSymbol, DateTime.MinValue, DateTime.UtcNow, cancellationToken);
			quote = await quoteManagement.GetQuoteAsync(investmentRequest, cancellationToken);
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
		await databaseContext.SaveChangesAsync(cancellationToken);

		return ApiResponse.Create(new InvestmentViewDto(investment), System.Net.HttpStatusCode.OK);
	}

	public async Task<ApiResponse> DeleteInvestmentAsync(int id, CancellationToken cancellationToken)
	{
		InvestmentModel? investment = await databaseContext.Investments.Where(i => i.UserId == userId && i.Id == id).FirstOrDefaultAsync(cancellationToken);

		if (investment is null)
			return ApiResponses.NotFound404;

		databaseContext.Investments.Remove(investment);
		await databaseContext.SaveChangesAsync(cancellationToken);

		return ApiResponses.NoContent204;
	}
}