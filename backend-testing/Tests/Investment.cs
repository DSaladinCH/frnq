using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;
using Moq;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Auth;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Investment")]
public class Investment : TestBase
{
	[Fact]
	public async Task GetInvestments_WhenAuthenticated_ReturnsListOfInvestments()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		TestResponse<PaginatedInvestmentsResponse> response = await ApiInterface.Investments.GetInvestments();

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotEmpty(response.Content?.Items ?? []);
	}

	[Fact]
	public async Task CreateInvestment_WithValidQuoteId_CreatesInDatabaseAndReturnsCreated()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		InvestmentDto investment = new InvestmentDto
		{
			QuoteId = 1,
			Date = DateTime.UtcNow.Date,
			Amount = 5,
			PricePerUnit = 160.0m,
			TotalFees = 2.0m,
			Type = InvestmentType.Buy
		};

		TestResponse response = await ApiInterface.Investments.CreateInvestment(investment);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		// Verify created in database
		InvestmentModel? createdInvestment = DbContext.Investments.FirstOrDefault(i => i.Amount == 5 && i.PricePerUnit == 160.0m);
		Assert.NotNull(createdInvestment);
		Assert.Equal(1, createdInvestment.QuoteId);
		Assert.Equal(InvestmentType.Buy, createdInvestment.Type);
	}

	[Fact]
	public async Task CreateInvestment_WithValidSymbolAndProvider_CreatesInDatabaseAndReturnsCreated()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		InvestmentDto investment = new InvestmentDto
		{
			QuoteSymbol = "AAPL",
			ProviderId = "yahoo-finance",
			Date = DateTime.UtcNow.Date,
			Amount = 5,
			PricePerUnit = 160.0m,
			TotalFees = 2.0m,
			Type = InvestmentType.Buy
		};

		TestResponse response = await ApiInterface.Investments.CreateInvestment(investment);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		// Verify created in database
		InvestmentModel? createdInvestment = DbContext.Investments.FirstOrDefault(i => i.Amount == 5 && i.Type == InvestmentType.Buy);
		Assert.NotNull(createdInvestment);
		Assert.Equal(1, createdInvestment.QuoteId); // AAPL has Id=1 from DataSeeder
	}

	[Fact]
	public async Task CreateInvestmentsBulk_WithValidData_CreatesAllInDatabaseAndReturnsCreated()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		List<InvestmentDto> investments = new List<InvestmentDto>
		{
			new() { QuoteId = 1, Date = DateTime.UtcNow.Date, Amount = 1, PricePerUnit = 150m, TotalFees = 1m, Type = InvestmentType.Buy }
		};

		TestResponse response = await ApiInterface.Investments.CreateInvestmentsBulk(investments);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		// Verify all created in database
		List<InvestmentModel> createdInvestments = DbContext.Investments.Where(i => i.Amount == 1 && i.PricePerUnit == 150m).ToList();
		Assert.NotEmpty(createdInvestments);
		Assert.Single(createdInvestments);
	}

	[Fact]
	public async Task UpdateInvestment_WithValidData_ModifiesDatabaseAndReturnsSuccess()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		// Setup: Create initial investment directly in database
		const int amount = 5;
		const decimal pricePerUnit = 150.0m;
		const decimal fees = 2.5m;
		await DbContext.Investments.AddAsync(new InvestmentModel
		{
			Id = 999,
			QuoteId = 1,
			UserId = DataSeeder.TestUserId,
			Date = DateTime.UtcNow.Date,
			Amount = amount,
			PricePerUnit = pricePerUnit,
			TotalFees = fees,
			Type = InvestmentType.Buy
		});
		await DbContext.SaveChangesAsync();

		InvestmentDto investment = new InvestmentDto
		{
			QuoteId = 1,
			Date = DateTime.UtcNow.Date,
			Amount = 10,
			PricePerUnit = 170.0m,
			TotalFees = 5.0m,
			Type = InvestmentType.Buy
		};

		TestResponse response = await ApiInterface.Investments.UpdateInvestment(999, investment);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		// Verify updated in database
		InvestmentModel? updatedInvestment = DbContext.Investments.FirstOrDefault(i => i.Id == 999);
		Assert.NotNull(updatedInvestment);
		Assert.Equal(investment.Amount, updatedInvestment.Amount);
		Assert.Equal(investment.PricePerUnit, updatedInvestment.PricePerUnit);
	}

	[Fact]
	public async Task DeleteInvestment_WithExistingId_RemovesFromDatabaseAndReturnsSuccess()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		// Setup: Create initial investment directly in database
		await DbContext.Investments.AddAsync(new InvestmentModel
		{
			Id = 999,
			QuoteId = 1,
			UserId = DataSeeder.TestUserId,
			Date = DateTime.UtcNow.Date,
			Amount = 5,
			PricePerUnit = 150.0m,
			TotalFees = 2.5m,
			Type = InvestmentType.Buy
		});
		await DbContext.SaveChangesAsync();

		TestResponse response = await ApiInterface.Investments.DeleteInvestment(999);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

		// Verify deleted from database
		InvestmentModel? deletedInvestment = DbContext.Investments.FirstOrDefault(i => i.Id == 999);
		Assert.Null(deletedInvestment);
	}

	[Fact]
	public async Task CreateInvestment_WithInvalidQuote_ReturnsNotFound()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		// Setup mock to return empty list or null for this symbol
		FinanceProviderMock
			.Setup(p => p.GetQuoteAsync(It.Is<string>(s => s == "NON"), It.IsAny<CancellationToken>()))
			.ReturnsAsync((QuoteModel?)null);

		InvestmentDto investment = new InvestmentDto
		{
			QuoteId = 0,
			ProviderId = "yahoo-finance",
			QuoteSymbol = "NON",
			Date = DateTime.UtcNow.Date,
			Amount = 5,
			PricePerUnit = 160.0m,
			TotalFees = 2.0m,
			Type = InvestmentType.Buy
		};

		TestResponse response = await ApiInterface.Investments.CreateInvestment(investment);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}
}
