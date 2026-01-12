using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;
using Moq;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Testing.Api;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Investment")]
public class Investment(CustomWebApplicationFactory<Program> factory) : BaseTest(factory)
{
    [Fact]
    public async Task GetInvestments_WhenAuthenticated_ReturnsListOfInvestments()
    {
        await AuthenticateAsync();
		TestResponse<PaginatedInvestmentsResponse> response = await Api.Investments.GetInvestments();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(response.Content?.Items ?? []);
    }

    [Fact]
    public async Task CreateInvestment_WithValidQuoteId_CreatesInDatabaseAndReturnsCreated()
    {
        await AuthenticateAsync();
		InvestmentDto investment = new InvestmentDto
        {
            QuoteId = 1,
            Date = DateTime.UtcNow.Date,
            Amount = 5,
            PricePerUnit = 160.0m,
            TotalFees = 2.0m,
            Type = InvestmentType.Buy
        };

		TestResponse response = await Api.Investments.CreateInvestment(investment);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		// Verify created in database
		InvestmentModel? createdInvestment = ExecuteWithDatabaseContext(context => 
            context.Investments.FirstOrDefault(i => i.Amount == 5 && i.PricePerUnit == 160.0m));
        Assert.NotNull(createdInvestment);
        Assert.Equal(1, createdInvestment.QuoteId);
        Assert.Equal(InvestmentType.Buy, createdInvestment.Type);
    }

    [Fact]
    public async Task CreateInvestment_WithValidSymbolAndProvider_CreatesInDatabaseAndReturnsCreated()
    {
        await AuthenticateAsync();

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

		TestResponse response = await Api.Investments.CreateInvestment(investment);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		// Verify created in database
		InvestmentModel? createdInvestment = ExecuteWithDatabaseContext(context => 
            context.Investments.FirstOrDefault(i => i.Amount == 5 && i.Type == InvestmentType.Buy));
        Assert.NotNull(createdInvestment);
        Assert.Equal(1, createdInvestment.QuoteId); // AAPL has Id=1 from DataSeeder
    }

    [Fact]
    public async Task CreateInvestmentsBulk_WithValidData_CreatesAllInDatabaseAndReturnsCreated()
    {
        await AuthenticateAsync();
		List<InvestmentDto> investments = new List<InvestmentDto>
        {
            new() { QuoteId = 1, Date = DateTime.UtcNow.Date, Amount = 1, PricePerUnit = 150m, TotalFees = 1m, Type = InvestmentType.Buy }
        };

		TestResponse response = await Api.Investments.CreateInvestmentsBulk(investments);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		// Verify all created in database
		List<InvestmentModel> createdInvestments = ExecuteWithDatabaseContext(context => 
            context.Investments.Where(i => i.Amount == 1 && i.PricePerUnit == 150m).ToList());
        Assert.NotEmpty(createdInvestments);
        Assert.Single(createdInvestments);
    }

    [Fact]
    public async Task UpdateInvestment_WithValidData_ModifiesDatabaseAndReturnsSuccess()
    {
        await AuthenticateAsync();
        
        // Setup: Create initial investment directly in database
        const int amount = 5;
        const decimal pricePerUnit = 150.0m;
        const decimal fees = 2.5m;
        using (DatabaseContext setupContext = GetDatabaseContext())
        {
            setupContext.Investments.Add(new InvestmentModel
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
            setupContext.SaveChanges();
        }

		InvestmentDto investment = new InvestmentDto
        {
            QuoteId = 1,
            Date = DateTime.UtcNow.Date,
            Amount = 10,
            PricePerUnit = 170.0m,
            TotalFees = 5.0m,
            Type = InvestmentType.Buy
        };

		TestResponse response = await Api.Investments.UpdateInvestment(999, investment);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		// Verify updated in database
		InvestmentModel? updatedInvestment = ExecuteWithDatabaseContext(context => 
            context.Investments.FirstOrDefault(i => i.Id == 999));
        Assert.NotNull(updatedInvestment);
        Assert.Equal(investment.Amount, updatedInvestment.Amount);
        Assert.Equal(investment.PricePerUnit, updatedInvestment.PricePerUnit);
    }

    [Fact]
    public async Task DeleteInvestment_WithExistingId_RemovesFromDatabaseAndReturnsSuccess()
    {
        await AuthenticateAsync();
        
        // Setup: Create initial investment directly in database
        using (DatabaseContext setupContext = GetDatabaseContext())
        {
            setupContext.Investments.Add(new InvestmentModel
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
            setupContext.SaveChanges();
        }

		TestResponse response = await Api.Investments.DeleteInvestment(999);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

		// Verify deleted from database
		InvestmentModel? deletedInvestment = ExecuteWithDatabaseContext(context => 
            context.Investments.FirstOrDefault(i => i.Id == 999));
        Assert.Null(deletedInvestment);
    }

    [Fact]
    public async Task CreateInvestment_WithInvalidQuote_ReturnsNotFound()
    {
        await AuthenticateAsync();
        
        // Setup mock to return empty list or null for this symbol
        Factory.FinanceProviderMock
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

		TestResponse response = await Api.Investments.CreateInvestment(investment);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
