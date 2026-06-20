using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.GeneralFee;
using DSaladin.Frnq.Api.Position;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Result;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Allure.Xunit.Attributes;
using Moq;
using System.Net;
using Xunit;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("GeneralFee")]
public class GeneralFee : TestBase
{
	private static readonly DateTime ValidDate = new(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

	private void SetupFinanceProviderMock()
	{
		FinanceProviderMock
			.Setup(p => p.GetHistoricalPricesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<QuotePrice>());
	}

	private int GetTestUserGroupId()
		=> DbContext.QuoteGroups.First(g => g.UserId == DataSeeder.TestUserId).Id;

	// ── Create ────────────────────────────────────────────────────────────────

	[Fact]
	public async Task CreateFee_PortfolioLevel_ReturnsCreatedFee()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse<GeneralFeeViewDto> response = await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto
		{
			Amount = 99.50m,
			Date = ValidDate,
			Description = "Advisory Fee"
		});

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		Assert.NotNull(response.Value);
		Assert.Equal(99.50m, response.Value.Amount);
		Assert.Equal("Advisory Fee", response.Value.Description);
		Assert.Null(response.Value.GroupId);
		Assert.Equal(ValidDate.Date, response.Value.Date.Date);
	}

	[Fact]
	public async Task CreateFee_WithGroup_SetsGroupId()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		int groupId = GetTestUserGroupId();

		ApiResponse<GeneralFeeViewDto> response = await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto
		{
			Amount = 25m,
			Date = ValidDate,
			Description = "Custody Fee",
			GroupId = groupId
		});

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		Assert.NotNull(response.Value);
		Assert.Equal(groupId, response.Value.GroupId);
	}

	[Fact]
	public async Task CreateFee_FutureDate_ReturnsBadRequest()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse<GeneralFeeViewDto> response = await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto
		{
			Amount = 10m,
			Date = DateTime.UtcNow.AddDays(1),
			Description = "Future Fee"
		});

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task CreateFee_ZeroAmount_ReturnsBadRequest()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse<GeneralFeeViewDto> response = await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto
		{
			Amount = 0m,
			Date = ValidDate,
			Description = "Zero Fee"
		});

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task CreateFee_NegativeAmount_ReturnsBadRequest()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse<GeneralFeeViewDto> response = await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto
		{
			Amount = -5m,
			Date = ValidDate,
			Description = "Negative Fee"
		});

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	// ── List / Pagination ─────────────────────────────────────────────────────

	[Fact]
	public async Task GetFees_ReturnsAllUserFees()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto { Amount = 10m, Date = ValidDate, Description = "Fee A" });
		await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto { Amount = 20m, Date = ValidDate, Description = "Fee B" });

		ApiResponse<PaginatedGeneralFeesResponse> response = await ApiInterface.GeneralFees.GetFees();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		Assert.Equal(2, response.Value.TotalCount);
		Assert.Equal(2, response.Value.Items.Count);
	}

	[Fact]
	public async Task GetFees_Pagination_SkipAndTakeWork()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		for (int i = 1; i <= 5; i++)
		{
			await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto
			{
				Amount = i * 10m,
				Date = ValidDate.AddDays(-i),
				Description = $"Fee {i}"
			});
		}

		ApiResponse<PaginatedGeneralFeesResponse> page1 = await ApiInterface.GeneralFees.GetFees(skip: 0, take: 2);
		ApiResponse<PaginatedGeneralFeesResponse> page2 = await ApiInterface.GeneralFees.GetFees(skip: 2, take: 2);
		ApiResponse<PaginatedGeneralFeesResponse> page3 = await ApiInterface.GeneralFees.GetFees(skip: 4, take: 2);

		Assert.Equal(HttpStatusCode.OK, page1.StatusCode);
		Assert.Equal(5, page1.Value!.TotalCount);
		Assert.Equal(2, page1.Value.Items.Count);

		Assert.Equal(5, page2.Value!.TotalCount);
		Assert.Equal(2, page2.Value.Items.Count);

		Assert.Equal(5, page3.Value!.TotalCount);
		Assert.Single(page3.Value.Items);

		// All three pages should return distinct fees
		var allIds = page1.Value.Items.Select(f => f.Id)
			.Concat(page2.Value.Items.Select(f => f.Id))
			.Concat(page3.Value.Items.Select(f => f.Id))
			.ToList();
		Assert.Equal(5, allIds.Distinct().Count());
	}

	[Fact]
	public async Task GetFees_FilteredByGroup_ReturnsOnlyGroupFees()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		int groupId = GetTestUserGroupId();

		await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto { Amount = 10m, Date = ValidDate, Description = "Portfolio Fee" });
		await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto { Amount = 20m, Date = ValidDate, Description = "Group Fee", GroupId = groupId });

		ApiResponse<PaginatedGeneralFeesResponse> response = await ApiInterface.GeneralFees.GetFees(groupId: groupId);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		Assert.Equal(1, response.Value.TotalCount);
		Assert.Single(response.Value.Items);
		Assert.Equal("Group Fee", response.Value.Items[0].Description);
	}

	// ── Update ────────────────────────────────────────────────────────────────

	[Fact]
	public async Task UpdateFee_ChangesAmountAndDescription()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse<GeneralFeeViewDto> created = await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto
		{
			Amount = 50m,
			Date = ValidDate,
			Description = "Original"
		});
		int feeId = created.Value!.Id;

		ApiResponse<GeneralFeeViewDto> updated = await ApiInterface.GeneralFees.UpdateFee(feeId, new GeneralFeeDto
		{
			Amount = 75m,
			Date = ValidDate,
			Description = "Updated"
		});

		Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
		Assert.NotNull(updated.Value);
		Assert.Equal(75m, updated.Value.Amount);
		Assert.Equal("Updated", updated.Value.Description);
	}

	[Fact]
	public async Task UpdateFee_FutureDate_ReturnsBadRequest()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse<GeneralFeeViewDto> created = await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto
		{
			Amount = 50m,
			Date = ValidDate,
			Description = "Fee"
		});

		ApiResponse<GeneralFeeViewDto> updated = await ApiInterface.GeneralFees.UpdateFee(created.Value!.Id, new GeneralFeeDto
		{
			Date = DateTime.UtcNow.AddDays(1)
		});

		Assert.Equal(HttpStatusCode.BadRequest, updated.StatusCode);
	}

	// ── Delete ────────────────────────────────────────────────────────────────

	[Fact]
	public async Task DeleteFee_RemovesFeeFromList()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse<GeneralFeeViewDto> created = await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto
		{
			Amount = 30m,
			Date = ValidDate,
			Description = "To Delete"
		});
		int feeId = created.Value!.Id;

		ApiResponse deleteResponse = await ApiInterface.GeneralFees.DeleteFee(feeId);
		Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

		ApiResponse<PaginatedGeneralFeesResponse> list = await ApiInterface.GeneralFees.GetFees();
		Assert.Equal(0, list.Value!.TotalCount);
		Assert.DoesNotContain(list.Value.Items, f => f.Id == feeId);
	}

	[Fact]
	public async Task DeleteFee_NonExistent_ReturnsBadRequest()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse response = await ApiInterface.GeneralFees.DeleteFee(999999);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	// ── Positions integration ─────────────────────────────────────────────────

	[Fact]
	public async Task FeesAppearsInPositionsResponse_OverallFees()
	{
		SetupFinanceProviderMock();
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserModel user = await DataSeeder.GetTestUser(DbContext);

		var feeDate = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc);

		// Seed investment + price so PositionManagement doesn't short-circuit before loading fees
		var quote = new QuoteModel
		{
			Id = 200,
			ProviderId = "yahoo-finance",
			Symbol = "FEE_TEST1",
			Name = "Fee Test 1",
			Currency = "CHF",
			ExchangeDisposition = "Test",
			TypeDisposition = "EQUITY"
		};
		DbContext.Quotes.Add(quote);
		DbContext.Investments.Add(new Frnq.Api.Investment.InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = feeDate,
			Amount = 10m,
			PricePerUnit = 100m,
			TotalFees = 0m,
			Type = Frnq.Api.Investment.InvestmentType.Buy
		});
		DbContext.QuotePrices.Add(new QuotePrice { QuoteId = quote.Id, Date = feeDate, Close = 100m });

		// Insert fee directly to bypass any serialization/timezone ambiguity in the API path
		DbContext.GeneralFees.Add(new GeneralFeeModel
		{
			UserId = user.Id,
			Date = feeDate,
			Amount = 123.45m,
			Description = "Portfolio Advisory Fee",
			CreatedAt = DateTime.UtcNow
		});
		await DbContext.SaveChangesAsync();

		ApiResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions(feeDate, feeDate);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		Assert.Equal(123.45m, response.Value.OverallFees);
	}

	[Fact]
	public async Task FeesAppearsInPositionsResponse_GroupFeesSummaries()
	{
		SetupFinanceProviderMock();
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserModel user = await DataSeeder.GetTestUser(DbContext);
		int groupId = GetTestUserGroupId();

		var feeDate = new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc);

		var quote = new QuoteModel
		{
			Id = 201,
			ProviderId = "yahoo-finance",
			Symbol = "FEE_TEST2",
			Name = "Fee Test 2",
			Currency = "CHF",
			ExchangeDisposition = "Test",
			TypeDisposition = "EQUITY"
		};
		DbContext.Quotes.Add(quote);
		DbContext.Investments.Add(new Frnq.Api.Investment.InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = feeDate,
			Amount = 10m,
			PricePerUnit = 100m,
			TotalFees = 0m,
			Type = Frnq.Api.Investment.InvestmentType.Buy
		});
		DbContext.QuotePrices.Add(new QuotePrice { QuoteId = quote.Id, Date = feeDate, Close = 100m });

		DbContext.GeneralFees.Add(new GeneralFeeModel
		{
			UserId = user.Id,
			Date = feeDate,
			Amount = 55m,
			Description = "Group Custody Fee",
			GroupId = groupId,
			CreatedAt = DateTime.UtcNow
		});
		await DbContext.SaveChangesAsync();

		ApiResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions(feeDate, feeDate);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		Assert.Equal(0m, response.Value.OverallFees);

		GroupFeesSummaryDto? summary = response.Value.GroupFeesSummaries.FirstOrDefault(g => g.GroupId == groupId);
		Assert.NotNull(summary);
		Assert.Equal(55m, summary.TotalGeneralFees);
		Assert.Single(summary.FeeDetails);
	}

	[Fact]
	public async Task FeesOutsideDateRange_NotIncludedInPositionsResponse()
	{
		SetupFinanceProviderMock();
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserModel user = await DataSeeder.GetTestUser(DbContext);

		var feeDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
		var queryDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);

		// Add investment + price on the query date so positions doesn't short-circuit
		var quote = new QuoteModel
		{
			Id = 202,
			ProviderId = "yahoo-finance",
			Symbol = "FEE_TEST3",
			Name = "Fee Test 3",
			Currency = "CHF",
			ExchangeDisposition = "Test",
			TypeDisposition = "EQUITY"
		};
		DbContext.Quotes.Add(quote);
		DbContext.Investments.Add(new Frnq.Api.Investment.InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = queryDate,
			Amount = 10m,
			PricePerUnit = 100m,
			TotalFees = 0m,
			Type = Frnq.Api.Investment.InvestmentType.Buy
		});
		DbContext.QuotePrices.Add(new QuotePrice { QuoteId = quote.Id, Date = queryDate, Close = 100m });

		// Fee is on Feb 1 but query is Feb 20 — should not be included
		DbContext.GeneralFees.Add(new GeneralFeeModel
		{
			UserId = user.Id,
			Date = feeDate,
			Amount = 200m,
			Description = "Out-of-range Fee",
			CreatedAt = DateTime.UtcNow
		});
		await DbContext.SaveChangesAsync();

		ApiResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions(queryDate, queryDate);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(0m, response.Value!.OverallFees);
		Assert.Empty(response.Value.GroupFeesSummaries);
	}

	// ── Authorization ─────────────────────────────────────────────────────────

	[Fact]
	public async Task GetFees_Unauthenticated_ReturnsUnauthorized()
	{
		ApiResponse<PaginatedGeneralFeesResponse> response = await ApiInterface.GeneralFees.GetFees();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task CreateFee_Unauthenticated_ReturnsUnauthorized()
	{
		ApiResponse<GeneralFeeViewDto> response = await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto
		{
			Amount = 10m,
			Date = ValidDate,
			Description = "Unauthorized Fee"
		});

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task GetFees_UserIsolation_DoesNotSeeOtherUsersFees()
	{
		// User 1 creates a fee
		using (AuthenticationScope<UserModel> authScope1 = await Authenticate())
		{
			await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto
			{
				Amount = 100m,
				Date = ValidDate,
				Description = "User 1 Fee"
			});
		}

		// User 2 should see an empty list
		UserModel user2 = await DataSeeder.GetTestUser2(DbContext);
		using AuthenticationScope<UserModel> authScope2 = Authenticate(user2);

		ApiResponse<PaginatedGeneralFeesResponse> response = await ApiInterface.GeneralFees.GetFees();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(0, response.Value!.TotalCount);
	}

	[Fact]
	public async Task DeleteFee_OtherUsersFee_ReturnsBadRequest()
	{
		// User 1 creates a fee
		int feeId;
		using (AuthenticationScope<UserModel> authScope1 = await Authenticate())
		{
			ApiResponse<GeneralFeeViewDto> created = await ApiInterface.GeneralFees.CreateFee(new GeneralFeeDto
			{
				Amount = 50m,
				Date = ValidDate,
				Description = "User 1 Private Fee"
			});
			feeId = created.Value!.Id;
		}

		// User 2 tries to delete it
		UserModel user2 = await DataSeeder.GetTestUser2(DbContext);
		using AuthenticationScope<UserModel> authScope2 = Authenticate(user2);

		ApiResponse deleteResponse = await ApiInterface.GeneralFees.DeleteFee(feeId);

		Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
	}
}
