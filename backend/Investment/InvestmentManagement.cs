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
			TotalFees = investmentRequest.TotalFees,
			ExcludeFromForecast = investmentRequest.ExcludeFromForecast
		};

		if (!await CanApplyInvestmentsAsync([investment], null, cancellationToken))
			return ApiResponse.Create("INSUFFICIENT_HOLDINGS", "A sell cannot exceed the shares held on its date.", System.Net.HttpStatusCode.BadRequest);

		await databaseContext.Investments.AddAsync(investment, cancellationToken);
		await databaseContext.SaveChangesAsync(cancellationToken);

		return ApiResponses.Created201;
	}

	public async Task<ApiResponse<List<InvestmentViewDto>>> CreateInvestmentsAsync(List<InvestmentDto> investmentRequests, CancellationToken cancellationToken)
	{
		List<InvestmentModel> investments = new List<InvestmentModel>();

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
				TotalFees = investmentRequest.TotalFees,
				ExcludeFromForecast = investmentRequest.ExcludeFromForecast
			};

			investments.Add(investment);
		}

		if (!await CanApplyInvestmentsAsync(investments, null, cancellationToken))
			return ApiResponse.Create<List<InvestmentViewDto>>("INSUFFICIENT_HOLDINGS", "A sell cannot exceed the shares held on its date.", System.Net.HttpStatusCode.BadRequest);

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

		InvestmentModel candidate = new()
		{
			Id = investment.Id,
			UserId = userId,
			QuoteId = quote.Id,
			Date = DateTime.SpecifyKind(investmentRequest.Date, DateTimeKind.Utc),
			Type = investmentRequest.Type,
			Amount = investmentRequest.Amount,
			PricePerUnit = investmentRequest.PricePerUnit,
			TotalFees = investmentRequest.TotalFees,
			ExcludeFromForecast = investmentRequest.ExcludeFromForecast
		};

		if (!await CanApplyInvestmentsAsync([candidate], investment.Id, cancellationToken))
			return ApiResponse.Create<InvestmentViewDto>("INSUFFICIENT_HOLDINGS", "A sell cannot exceed the shares held on its date.", System.Net.HttpStatusCode.BadRequest);

		investment.QuoteId = candidate.QuoteId;
		investment.Date = candidate.Date;
		investment.Type = candidate.Type;
		investment.Amount = candidate.Amount;
		investment.PricePerUnit = candidate.PricePerUnit;
		investment.TotalFees = candidate.TotalFees;
		investment.ExcludeFromForecast = candidate.ExcludeFromForecast;

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

	private async Task<bool> CanApplyInvestmentsAsync(IReadOnlyCollection<InvestmentModel> candidates, int? replacementId, CancellationToken cancellationToken)
	{
		HashSet<int> quoteIds = candidates.Select(i => i.QuoteId).ToHashSet();
		if (replacementId.HasValue)
		{
			int? originalQuoteId = await databaseContext.Investments
				.Where(i => i.Id == replacementId.Value)
				.Select(i => (int?)i.QuoteId)
				.FirstOrDefaultAsync(cancellationToken);
			if (originalQuoteId.HasValue)
				quoteIds.Add(originalQuoteId.Value);
		}

		List<InvestmentModel> investments = await databaseContext.Investments
			.AsNoTracking()
			.Where(i => i.UserId == userId && quoteIds.Contains(i.QuoteId) && (!replacementId.HasValue || i.Id != replacementId.Value))
			.ToListAsync(cancellationToken);
		investments.AddRange(candidates);

		foreach (IGrouping<int, InvestmentModel> quoteInvestments in investments.GroupBy(i => i.QuoteId))
		{
			decimal holdings = 0;
			foreach (InvestmentModel investment in quoteInvestments.OrderBy(i => i.Date).ThenBy(i => i.Id))
			{
				if (investment.Type == InvestmentType.Buy)
					holdings += investment.Amount;
				else if (investment.Type == InvestmentType.Sell)
				{
					if (investment.Amount > holdings)
						return false;
					holdings -= investment.Amount;
				}
			}
		}

		return true;
	}
}