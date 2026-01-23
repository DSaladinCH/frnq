using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;
using Moq;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Investment")]
public class Investment : TestBase
{
	[Fact]
	public async Task GetInvestments()
	{
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		InvestmentModel investment = DbContext.Investments
			.Include(i => i.Quote)
			.ThenInclude(q => q.Mappings.Where(m => m.UserId == DataSeeder.TestUserId))
			.First(i => i.UserId == DataSeeder.TestUserId);

		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse<PaginatedInvestmentsResponse> response = await ApiInterface.Investments.GetInvestments(0, 25);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(investmentCountBefore, response.Value!.TotalCount);
	}

	[Fact]
	public async Task GetInvestmentsFiltered()
	{
		InvestmentModel investment = DbContext.Investments
			.Include(i => i.Quote)
			.ThenInclude(q => q.Mappings.Where(m => m.UserId == DataSeeder.TestUserId))
			.First(i => i.UserId == DataSeeder.TestUserId);

		int investmentCountBefore = DbContext.Investments.Where(i =>
				i.UserId == DataSeeder.TestUserId &&
				i.Date >= DateTime.UtcNow.AddYears(-1) &&
				i.Date <= DateTime.UtcNow &&
				i.QuoteId == investment.QuoteId &&
				i.Quote.Mappings.First().GroupId == investment.Quote.Mappings.First().GroupId &&
				i.Type == investment.Type
			)
			.Count();

		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse<PaginatedInvestmentsResponse> response = await ApiInterface.Investments.GetInvestments(
			0, 25,
			DateTime.UtcNow.AddYears(-1),
			DateTime.UtcNow,
			investment.QuoteId,
			investment.Quote.Mappings.First().GroupId,
			investment.Type);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(investmentCountBefore, response.Value!.TotalCount);
	}
	
	[Fact]
	public async Task GetInvestmentsUnauthenticated()
	{
		ApiResponse<PaginatedInvestmentsResponse> response = await ApiInterface.Investments.GetInvestments(0, 25);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Null(response.Value);
	}

	[Fact]
	public async Task GetInvestmentById()
	{
		InvestmentModel investment = DbContext.Investments.First(i => i.UserId == DataSeeder.TestUserId);

		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse<InvestmentViewDto> response = await ApiInterface.Investments.GetInvestmentById(investment.Id);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(investment.UserId, response.Value!.UserId);
		Assert.Equal(investment.Id, response.Value!.Id);
		Assert.Equal(investment.QuoteId, response.Value!.QuoteId);
		Assert.Equal(investment.Date, response.Value!.Date);
		Assert.Equal(investment.Type, response.Value!.Type);
		Assert.Equal(investment.Amount, response.Value!.Amount);
		Assert.Equal(investment.PricePerUnit, response.Value!.PricePerUnit);
		Assert.Equal(investment.TotalFees, response.Value!.TotalFees);
	}

	[Fact]
	public async Task GetInvestmentByIdInvalid()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse<InvestmentViewDto> response = await ApiInterface.Investments.GetInvestmentById(999);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}
	
	[Fact]
	public async Task GetInvestmentByIdInvalidUnauthenticated()
	{
		ApiResponse<InvestmentViewDto> response = await ApiInterface.Investments.GetInvestmentById(0);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Null(response.Value);
	}

	[Theory]
	[InlineData("2023-01-15", 10, 150.0, 2.0, InvestmentType.Buy)]
	[InlineData("2023-01-15", 10, 150.0, 0, InvestmentType.Buy)] // Buy with zero fees
	[InlineData("2022-12-01", 5, 200.5, 1.5, InvestmentType.Sell)]
	[InlineData("2022-12-01", 5, 200.5, 0, InvestmentType.Sell)] // Sell with zero fees
	[InlineData("2023-03-10", 3.25, 0.0, 0.0, InvestmentType.Dividend)]
	public async Task CreateInvestmentValid(DateTime date, decimal amount, decimal pricePerUnit, decimal totalFees, InvestmentType type)
	{
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		QuoteModel quote = DbContext.Quotes.First();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		InvestmentDto investment = new()
		{
			QuoteId = quote.Id,
			Date = date,
			Type = type,
			Amount = amount,
			PricePerUnit = pricePerUnit,
			TotalFees = totalFees
		};

		ApiResponse response = await ApiInterface.Investments.CreateInvestment(investment);

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		InvestmentModel? createdInvestment = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Last();
		Assert.NotNull(createdInvestment);
		Assert.Equal(investmentCountBefore + 1, DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count());

		Assert.Equal(DataSeeder.TestUserId, createdInvestment.UserId);
		Assert.Equal(investment.QuoteId, createdInvestment.QuoteId);
		Assert.Equal(investment.Date, createdInvestment.Date);
		Assert.Equal(investment.Type, createdInvestment.Type);
		Assert.Equal(investment.Amount, createdInvestment.Amount);
		Assert.Equal(investment.PricePerUnit, createdInvestment.PricePerUnit);
		Assert.Equal(investment.TotalFees, createdInvestment.TotalFees);
	}

	[Fact]
	public async Task CreateInvestmentValidSymbol()
	{
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		QuoteModel quote = DbContext.Quotes.First();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		InvestmentDto investment = new()
		{
			ProviderId = quote.ProviderId,
			QuoteSymbol = quote.Symbol,
			Date = DateTime.UtcNow.Date,
			Type = InvestmentType.Buy,
			Amount = 10,
			PricePerUnit = 150.0m,
			TotalFees = 2.0m
		};

		ApiResponse response = await ApiInterface.Investments.CreateInvestment(investment);

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		InvestmentModel? createdInvestment = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Last();
		Assert.NotNull(createdInvestment);
		Assert.Equal(investmentCountBefore + 1, DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count());

		Assert.Equal(DataSeeder.TestUserId, createdInvestment.UserId);
		Assert.Equal(quote.Id, createdInvestment.QuoteId);
		Assert.Equal(investment.Date, createdInvestment.Date);
		Assert.Equal(investment.Type, createdInvestment.Type);
		Assert.Equal(investment.Amount, createdInvestment.Amount);
		Assert.Equal(investment.PricePerUnit, createdInvestment.PricePerUnit);
		Assert.Equal(investment.TotalFees, createdInvestment.TotalFees);
	}

	[Theory]
	[InlineData("0001-01-01", 10, 150.0, 2.0, InvestmentType.Buy)] // Invalid date
	[InlineData("2023-01-15", 0, 150.0, 2.0, InvestmentType.Buy)] // Zero amount
	[InlineData("2023-01-15", -5, 150.0, 2.0, InvestmentType.Buy)] // Negative amount
	[InlineData("2023-01-15", 10, -150.0, 2.0, InvestmentType.Buy)] // Negative price per unit
	[InlineData("2023-01-15", 10, 150.0, -2.0, InvestmentType.Buy)] // Negative total fees
	public async Task CreateInvestmentInvalid(DateTime date, decimal amount, decimal pricePerUnit, decimal totalFees, InvestmentType type)
	{
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		QuoteModel quote = DbContext.Quotes.First();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		InvestmentDto investment = new()
		{
			QuoteId = quote.Id,
			Date = date,
			Type = type,
			Amount = amount,
			PricePerUnit = pricePerUnit,
			TotalFees = totalFees
		};

		ApiResponse response = await ApiInterface.Investments.CreateInvestment(investment);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		InvestmentModel? createdInvestment = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Last();
		Assert.NotNull(createdInvestment);
		Assert.Equal(investmentCountBefore, DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task CreateInvestmentInvalidSymbol()
	{
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		// Setup mock to return empty list or null for this symbol
		FinanceProviderMock
			.Setup(p => p.GetQuoteAsync(It.Is<string>(s => s == "NON"), It.IsAny<CancellationToken>()))
			.ReturnsAsync((QuoteModel?)null);

		InvestmentDto investment = new()
		{
			ProviderId = "yahoo-finance",
			QuoteSymbol = "NON",
			Date = DateTime.UtcNow.Date,
			Type = InvestmentType.Buy,
			Amount = 10,
			PricePerUnit = 150.0m,
			TotalFees = 2.0m
		};

		ApiResponse response = await ApiInterface.Investments.CreateInvestment(investment);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		InvestmentModel? createdInvestment = DbContext.Investments.Include(i => i.Quote).Where(i => i.UserId == DataSeeder.TestUserId).Last();
		Assert.NotNull(createdInvestment);
		Assert.NotEqual(investment.QuoteSymbol, createdInvestment.Quote.Symbol);
		Assert.Equal(investmentCountBefore, DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count());
	}
	
	[Fact]
	public async Task CreateInvestmentUnauthenticated()
	{
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();

		InvestmentDto investment = new()
		{
			ProviderId = "yahoo-finance",
			QuoteSymbol = "NON",
			Date = DateTime.UtcNow.Date,
			Type = InvestmentType.Buy,
			Amount = 10,
			PricePerUnit = 150.0m,
			TotalFees = 2.0m
		};

		ApiResponse response = await ApiInterface.Investments.CreateInvestment(investment);
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		InvestmentModel? createdInvestment = DbContext.Investments.Include(i => i.Quote).Where(i => i.UserId == DataSeeder.TestUserId).Last();
		Assert.NotNull(createdInvestment);
		Assert.NotEqual(investment.QuoteSymbol, createdInvestment.Quote.Symbol);
		Assert.Equal(investmentCountBefore, DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task CreateInvestmentsBulkValid()
	{
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		QuoteModel quote = DbContext.Quotes.First();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		List<InvestmentDto> investments =
		[
			new() { QuoteId = quote.Id, Date = DateTime.UtcNow.Date.AddDays(-2), Amount = 5, PricePerUnit = 100m, TotalFees = 1m, Type = InvestmentType.Buy },
			new() { QuoteId = quote.Id, Date = DateTime.UtcNow.Date.AddDays(-1), Amount = 10, PricePerUnit = 150m, TotalFees = 2m, Type = InvestmentType.Sell },
			new() { QuoteId = quote.Id, Date = DateTime.UtcNow.Date, Amount = 3, PricePerUnit = 200m, TotalFees = 0.5m, Type = InvestmentType.Dividend }
		];

		ApiResponse response = await ApiInterface.Investments.CreateInvestmentsBulk(investments);

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		int investmentCountAfter = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		Assert.Equal(investmentCountBefore + 3, investmentCountAfter);
	}

	[Fact]
	public async Task CreateInvestmentsBulkInvalidSymbol()
	{
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		QuoteModel quote = DbContext.Quotes.First();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		// Setup mock to return empty list or null for this symbol
		FinanceProviderMock
			.Setup(p => p.GetQuoteAsync(It.Is<string>(s => s == "NON"), It.IsAny<CancellationToken>()))
			.ReturnsAsync((QuoteModel?)null);

		List<InvestmentDto> investments =
		[
			new() { ProviderId = "yahoo-finance", QuoteSymbol = "NON", Date = DateTime.UtcNow.Date.AddDays(-2), Amount = 5, PricePerUnit = 100m, TotalFees = 1m, Type = InvestmentType.Buy },
		];

		ApiResponse response = await ApiInterface.Investments.CreateInvestmentsBulk(investments);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		int investmentCountAfter = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		Assert.Equal(investmentCountBefore, investmentCountAfter);
	}

	[Fact]
	public async Task CreateInvestmentsBulkInvalid()
	{
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		QuoteModel quote = DbContext.Quotes.First();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		List<InvestmentDto> investments =
		[
			new() { QuoteId = quote.Id, Date = DateTime.UtcNow.Date, Amount = -5, PricePerUnit = 100m, TotalFees = 1m, Type = InvestmentType.Buy },
			new() { QuoteId = 999999, Date = DateTime.UtcNow.Date, Amount = 10, PricePerUnit = 150m, TotalFees = 2m, Type = InvestmentType.Sell }
		];

		ApiResponse response = await ApiInterface.Investments.CreateInvestmentsBulk(investments);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		int investmentCountAfter = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		Assert.Equal(investmentCountBefore, investmentCountAfter);
	}

	[Fact]
	public async Task CreateInvestmentsBulkUnauthenticated()
	{
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		QuoteModel quote = DbContext.Quotes.First();

		List<InvestmentDto> investments =
		[
			new() { QuoteId = quote.Id, Date = DateTime.UtcNow.Date, Amount = 5, PricePerUnit = 100m, TotalFees = 1m, Type = InvestmentType.Buy }
		];

		ApiResponse response = await ApiInterface.Investments.CreateInvestmentsBulk(investments);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		int investmentCountAfter = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		Assert.Equal(investmentCountBefore, investmentCountAfter);
	}

	[Theory]
	[InlineData("2022-12-01", 15, 200.0, 2.0, InvestmentType.Buy)]
	[InlineData("2022-12-01", 15, 200.0, 0, InvestmentType.Buy)] // Buy with zero fees
	[InlineData("2023-01-15", 20, 175.0, 3.5, InvestmentType.Sell)]
	[InlineData("2023-01-15", 20, 175.0, 0, InvestmentType.Sell)] // Sell with zero fees
	[InlineData("2023-03-10", 5.5, 0, 0, InvestmentType.Dividend)]
	public async Task UpdateInvestmentValid(DateTime date, decimal amount, decimal pricePerUnit, decimal totalFees, InvestmentType type)
	{
		InvestmentModel existingInvestment = DbContext.Investments.First(i => i.UserId == DataSeeder.TestUserId);
		QuoteModel quote = DbContext.Quotes.First();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		InvestmentDto updatedInvestment = new()
		{
			QuoteId = quote.Id,
			Date = date,
			Amount = amount,
			PricePerUnit = pricePerUnit,
			TotalFees = totalFees,
			Type = type
		};

		ApiResponse response = await ApiInterface.Investments.UpdateInvestment(existingInvestment.Id, updatedInvestment);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		InvestmentModel? verifyInvestment = DbContext.Investments.FirstOrDefault(i => i.Id == existingInvestment.Id);
		Assert.NotNull(verifyInvestment);
		Assert.Equal(updatedInvestment.QuoteId, verifyInvestment.QuoteId);
		Assert.Equal(updatedInvestment.Date, verifyInvestment.Date);
		Assert.Equal(updatedInvestment.Amount, verifyInvestment.Amount);
		Assert.Equal(updatedInvestment.PricePerUnit, verifyInvestment.PricePerUnit);
		Assert.Equal(updatedInvestment.TotalFees, verifyInvestment.TotalFees);
		Assert.Equal(updatedInvestment.Type, verifyInvestment.Type);
	}

	[Fact]
	public async Task UpdateInvestmentInvalidSymbol()
	{
		InvestmentModel existingInvestment = DbContext.Investments.First(i => i.UserId == DataSeeder.TestUserId);

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		// Setup mock to return empty list or null for this symbol
		FinanceProviderMock
			.Setup(p => p.GetQuoteAsync(It.Is<string>(s => s == "NON"), It.IsAny<CancellationToken>()))
			.ReturnsAsync((QuoteModel?)null);

		InvestmentDto updatedInvestment = new()
		{
			ProviderId = "yahoo-finance",
			QuoteSymbol = "NON",
			Date = DateTime.UtcNow.Date,
			Amount = 10,
			PricePerUnit = 170.0m,
			TotalFees = 5.0m,
			Type = InvestmentType.Buy
		};

		ApiResponse response = await ApiInterface.Investments.UpdateInvestment(existingInvestment.Id, updatedInvestment);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		InvestmentModel? verifyInvestment = DbContext.Investments.FirstOrDefault(i => i.Id == existingInvestment.Id);
		Assert.NotNull(verifyInvestment);
		Assert.Equal(existingInvestment.QuoteId, verifyInvestment.QuoteId);
		Assert.Equal(existingInvestment.Date, verifyInvestment.Date);
		Assert.Equal(existingInvestment.Amount, verifyInvestment.Amount);
		Assert.Equal(existingInvestment.PricePerUnit, verifyInvestment.PricePerUnit);
		Assert.Equal(existingInvestment.TotalFees, verifyInvestment.TotalFees);
		Assert.Equal(existingInvestment.Type, verifyInvestment.Type);
	}

	[Fact]
	public async Task UpdateInvestmentInvalidId()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		InvestmentDto updatedInvestment = new()
		{
			QuoteId = 1,
			Date = DateTime.UtcNow.Date,
			Amount = 10,
			PricePerUnit = 170.0m,
			TotalFees = 5.0m,
			Type = InvestmentType.Buy
		};

		ApiResponse response = await ApiInterface.Investments.UpdateInvestment(999999, updatedInvestment);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Theory]
	[InlineData("0001-01-01", 10, 170.0, 5.0, InvestmentType.Buy)] // Invalid date
	[InlineData("2023-01-15", 0, 170.0, 5.0, InvestmentType.Buy)] // Zero amount
	[InlineData("2023-01-15", -10, 170.0, 5.0, InvestmentType.Buy)] // Negative amount
	[InlineData("2023-01-15", 10, -170.0, 5.0, InvestmentType.Buy)] // Negative price per unit
	[InlineData("2023-01-15", 10, 170.0, -5.0, InvestmentType.Buy)] // Negative total fees
	public async Task UpdateInvestmentInvalidData(DateTime date, decimal amount, decimal pricePerUnit, decimal totalFees, InvestmentType type)
	{
		InvestmentModel existingInvestment = DbContext.Investments.First(i => i.UserId == DataSeeder.TestUserId);

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		InvestmentDto updatedInvestment = new()
		{
			QuoteId = 1,
			Date = date,
			Amount = amount,
			PricePerUnit = pricePerUnit,
			TotalFees = totalFees,
			Type = type
		};

		ApiResponse response = await ApiInterface.Investments.UpdateInvestment(existingInvestment.Id, updatedInvestment);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		InvestmentModel? verifyInvestment = DbContext.Investments.FirstOrDefault(i => i.Id == existingInvestment.Id);
		Assert.NotNull(verifyInvestment);
		Assert.Equal(existingInvestment.QuoteId, verifyInvestment.QuoteId);
		Assert.Equal(existingInvestment.Date, verifyInvestment.Date);
		Assert.Equal(existingInvestment.Amount, verifyInvestment.Amount);
		Assert.Equal(existingInvestment.PricePerUnit, verifyInvestment.PricePerUnit);
		Assert.Equal(existingInvestment.TotalFees, verifyInvestment.TotalFees);
		Assert.Equal(existingInvestment.Type, verifyInvestment.Type);
	}

	[Fact]
	public async Task UpdateInvestmentOtherUser()
	{
		InvestmentModel existingInvestment = DbContext.Investments.First(i => i.UserId == DataSeeder.TestUser2Id);

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		// Setup mock to return empty list or null for this symbol
		FinanceProviderMock
			.Setup(p => p.GetQuoteAsync(It.Is<string>(s => s == "NON"), It.IsAny<CancellationToken>()))
			.ReturnsAsync((QuoteModel?)null);

		InvestmentDto updatedInvestment = new()
		{
			ProviderId = "yahoo-finance",
			QuoteSymbol = "NON",
			Date = DateTime.UtcNow.Date,
			Amount = 10,
			PricePerUnit = 170.0m,
			TotalFees = 5.0m,
			Type = InvestmentType.Buy
		};

		ApiResponse response = await ApiInterface.Investments.UpdateInvestment(existingInvestment.Id, updatedInvestment);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		InvestmentModel? verifyInvestment = DbContext.Investments.FirstOrDefault(i => i.Id == existingInvestment.Id);
		Assert.NotNull(verifyInvestment);
		Assert.Equal(existingInvestment.QuoteId, verifyInvestment.QuoteId);
		Assert.Equal(existingInvestment.Date, verifyInvestment.Date);
		Assert.Equal(existingInvestment.Amount, verifyInvestment.Amount);
		Assert.Equal(existingInvestment.PricePerUnit, verifyInvestment.PricePerUnit);
		Assert.Equal(existingInvestment.TotalFees, verifyInvestment.TotalFees);
		Assert.Equal(existingInvestment.Type, verifyInvestment.Type);
	}

	[Fact]
	public async Task UpdateInvestmentUnauthenticated()
	{
		InvestmentModel existingInvestment = DbContext.Investments.First(i => i.UserId == DataSeeder.TestUserId);

		InvestmentDto updatedInvestment = new()
		{
			QuoteId = 1,
			Date = DateTime.UtcNow.Date,
			Amount = 10,
			PricePerUnit = 170.0m,
			TotalFees = 5.0m,
			Type = InvestmentType.Buy
		};

		ApiResponse response = await ApiInterface.Investments.UpdateInvestment(existingInvestment.Id, updatedInvestment);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		InvestmentModel? verifyInvestment = DbContext.Investments.FirstOrDefault(i => i.Id == existingInvestment.Id);
		Assert.NotNull(verifyInvestment);
		Assert.Equal(existingInvestment.QuoteId, verifyInvestment.QuoteId);
		Assert.Equal(existingInvestment.Date, verifyInvestment.Date);
		Assert.Equal(existingInvestment.Amount, verifyInvestment.Amount);
		Assert.Equal(existingInvestment.PricePerUnit, verifyInvestment.PricePerUnit);
		Assert.Equal(existingInvestment.TotalFees, verifyInvestment.TotalFees);
		Assert.Equal(existingInvestment.Type, verifyInvestment.Type);
	}

	[Fact]
	public async Task DeleteInvestmentValid()
	{
		InvestmentModel existingInvestment = DbContext.Investments.First(i => i.UserId == DataSeeder.TestUserId);
		int investmentId = existingInvestment.Id;
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse response = await ApiInterface.Investments.DeleteInvestment(investmentId);

		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		InvestmentModel? deletedInvestment = DbContext.Investments.FirstOrDefault(i => i.Id == investmentId);
		Assert.Null(deletedInvestment);
		Assert.Equal(investmentCountBefore - 1, DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task DeleteInvestmentInvalidId()
	{
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse response = await ApiInterface.Investments.DeleteInvestment(999999);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		Assert.Equal(investmentCountBefore, DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task DeleteInvestmentUnauthenticated()
	{
		int investmentCountBefore = DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count();
		InvestmentModel existingInvestment = DbContext.Investments.First(i => i.UserId == DataSeeder.TestUserId);

		ApiResponse response = await ApiInterface.Investments.DeleteInvestment(existingInvestment.Id);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		InvestmentModel? verifyInvestment = DbContext.Investments.FirstOrDefault(i => i.Id == existingInvestment.Id);
		Assert.NotNull(verifyInvestment);
		Assert.Equal(investmentCountBefore, DbContext.Investments.Where(i => i.UserId == DataSeeder.TestUserId).Count());
	}
}
