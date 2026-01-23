using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Position;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Result;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote;
using Moq;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Position")]
public class Position : TestBase
{
	private void SetupFinanceProviderMock()
	{
		FinanceProviderMock
			.Setup(p => p.GetHistoricalPricesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<QuotePrice>());
	}

	[Fact]
	public async Task GetPositionsSingleBuy()
	{
		SetupFinanceProviderMock();
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserModel user = await DataSeeder.GetTestUser(DbContext);
		
		var quote = new QuoteModel
		{
			Id = 100,
			ProviderId = "yahoo-finance",
			Symbol = "TEST1",
			Name = "Test Single Buy",
			Currency = "USD",
			ExchangeDisposition = "Test",
			TypeDisposition = "EQUITY"
		};
		DbContext.Quotes.Add(quote);
		
		var buyDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = buyDate,
			Amount = 100m,
			PricePerUnit = 50m,
			TotalFees = 10m,
			Type = InvestmentType.Buy
		});
		
		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc),
			Close = 55m
		});
		
		await DbContext.SaveChangesAsync();

		var from = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc);
		var to = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc);
		ApiResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions(from, to);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		
		PositionSnapshot? snapshot = response.Value.Snapshots.FirstOrDefault(s => s.QuoteId == quote.Id);
		Assert.NotNull(snapshot);
		Assert.Equal(100m, snapshot.Amount);
		Assert.Equal(5010m, snapshot.Invested);
		Assert.Equal(5010m, snapshot.TotalInvestedCash);
		Assert.Equal(0m, snapshot.RealizedGain);
		Assert.Equal(10m, snapshot.TotalFees);
		Assert.Equal(55m, snapshot.MarketPricePerUnit);
		Assert.Equal(5500m, snapshot.CurrentValue);
		Assert.Equal(490m, snapshot.UnrealizedGain);
	}

	[Fact]
	public async Task GetPositionsBuyDividend()
	{
		SetupFinanceProviderMock();
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserModel user = await DataSeeder.GetTestUser(DbContext);
		
		var quote = new QuoteModel
		{
			Id = 101,
			ProviderId = "yahoo-finance",
			Symbol = "TEST2",
			Name = "Test Dividend",
			Currency = "USD",
			ExchangeDisposition = "Test",
			TypeDisposition = "EQUITY"
		};
		DbContext.Quotes.Add(quote);
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc),
			Amount = 200m,
			PricePerUnit = 40m,
			TotalFees = 15m,
			Type = InvestmentType.Buy
		});
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
			Amount = 300m,
			PricePerUnit = 0m,
			TotalFees = 0m,
			Type = InvestmentType.Dividend
		});
		
		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
			Close = 42m
		});
		
		await DbContext.SaveChangesAsync();

		var from = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
		var to = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
		ApiResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions(from, to);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		
		PositionSnapshot? snapshot = response.Value.Snapshots.FirstOrDefault(s => s.QuoteId == quote.Id);
		Assert.NotNull(snapshot);
		Assert.Equal(200m, snapshot.Amount);
		Assert.Equal(8015m, snapshot.Invested);
		Assert.Equal(8015m, snapshot.TotalInvestedCash);
		Assert.Equal(300m, snapshot.RealizedGain);
		Assert.Equal(15m, snapshot.TotalFees);
		Assert.Equal(42m, snapshot.MarketPricePerUnit);
		Assert.Equal(8400m, snapshot.CurrentValue);
		Assert.Equal(385m, snapshot.UnrealizedGain);
		Assert.Equal(685m, snapshot.TotalProfit);
	}

	[Fact]
	public async Task GetPositionsFIFOPartialSell()
	{
		SetupFinanceProviderMock();
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserModel user = await DataSeeder.GetTestUser(DbContext);
		
		var quote = new QuoteModel
		{
			Id = 102,
			ProviderId = "yahoo-finance",
			Symbol = "TEST3",
			Name = "Test FIFO",
			Currency = "USD",
			ExchangeDisposition = "Test",
			TypeDisposition = "EQUITY"
		};
		DbContext.Quotes.Add(quote);
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc),
			Amount = 100m,
			PricePerUnit = 30m,
			TotalFees = 5m,
			Type = InvestmentType.Buy
		});
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc),
			Amount = 150m,
			PricePerUnit = 35m,
			TotalFees = 7.50m,
			Type = InvestmentType.Buy
		});
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc),
			Amount = 120m,
			PricePerUnit = 45m,
			TotalFees = 6m,
			Type = InvestmentType.Sell
		});
		
		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 13, 0, 0, 0, DateTimeKind.Utc),
			Close = 46m
		});
		
		await DbContext.SaveChangesAsync();

		var from = new DateTime(2026, 3, 13, 0, 0, 0, DateTimeKind.Utc);
		var to = new DateTime(2026, 3, 13, 0, 0, 0, DateTimeKind.Utc);
		ApiResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions(from, to);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		
		PositionSnapshot? snapshot = response.Value.Snapshots.FirstOrDefault(s => s.QuoteId == quote.Id);
		Assert.NotNull(snapshot);
		Assert.Equal(130m, snapshot.Amount);
		Assert.Equal(4556.50m, snapshot.Invested);
		Assert.Equal(8262.50m, snapshot.TotalInvestedCash);
		Assert.Equal(1688m, snapshot.RealizedGain);
		// TotalFees tracks cumulative fees across all transactions (5 + 7.50 + 6)
		Assert.Equal(18.50m, snapshot.TotalFees);
		Assert.Equal(46m, snapshot.MarketPricePerUnit);
		Assert.Equal(5980m, snapshot.CurrentValue);
		Assert.Equal(1423.50m, snapshot.UnrealizedGain);
		Assert.Equal(3111.50m, snapshot.TotalProfit);
	}

	[Fact]
	public async Task GetPositionsFIFOMultipleLots()
	{
		SetupFinanceProviderMock();
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserModel user = await DataSeeder.GetTestUser(DbContext);
		
		var quote = new QuoteModel
		{
			Id = 103,
			ProviderId = "yahoo-finance",
			Symbol = "TEST4",
			Name = "Test FIFO Cross Lot",
			Currency = "USD",
			ExchangeDisposition = "Test",
			TypeDisposition = "EQUITY"
		};
		DbContext.Quotes.Add(quote);
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 4, 0, 0, 0, DateTimeKind.Utc),
			Amount = 50m,
			PricePerUnit = 20m,
			TotalFees = 10m,
			Type = InvestmentType.Buy
		});
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc),
			Amount = 100m,
			PricePerUnit = 25m,
			TotalFees = 12m,
			Type = InvestmentType.Buy
		});
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc),
			Amount = 80m,
			PricePerUnit = 30m,
			TotalFees = 8m,
			Type = InvestmentType.Sell
		});
		
		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc),
			Close = 32m
		});
		
		await DbContext.SaveChangesAsync();

		var from = new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc);
		var to = new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc);
		ApiResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions(from, to);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		
		PositionSnapshot? snapshot = response.Value.Snapshots.FirstOrDefault(s => s.QuoteId == quote.Id);
		Assert.NotNull(snapshot);
		Assert.Equal(70m, snapshot.Amount);
		Assert.Equal(1758.40m, snapshot.Invested);
		Assert.Equal(3522m, snapshot.TotalInvestedCash);
		Assert.Equal(628.40m, snapshot.RealizedGain);
	}

	[Fact]
	public async Task GetPositionsDateRangeFiltered()
	{
		SetupFinanceProviderMock();
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserModel user = await DataSeeder.GetTestUser(DbContext);
		
		var quote = new QuoteModel
		{
			Id = 104,
			ProviderId = "yahoo-finance",
			Symbol = "TEST5",
			Name = "Test Date Range",
			Currency = "USD",
			ExchangeDisposition = "Test",
			TypeDisposition = "EQUITY"
		};
		DbContext.Quotes.Add(quote);
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc),
			Amount = 100m,
			PricePerUnit = 50m,
			TotalFees = 5m,
			Type = InvestmentType.Buy
		});
		
		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc),
			Close = 51m
		});
		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
			Close = 52m
		});
		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
			Close = 53m
		});
		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc),
			Close = 54m
		});
		
		await DbContext.SaveChangesAsync();

		var from = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc);
		var to = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);
		ApiResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions(from, to);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		
		var snapshots = response.Value.Snapshots.Where(s => s.QuoteId == quote.Id).ToList();
		
		// Should have 6 snapshots (one for each day from Feb 10-15 inclusive)
		Assert.Equal(6, snapshots.Count);
		Assert.All(snapshots, s =>
		{
			Assert.True(s.Date >= from);
			Assert.True(s.Date <= to);
		});
		// Verify that snapshots for Feb 10 and Feb 15 exist with correct prices
		var snapshot10 = snapshots.First(s => s.Date.Date == new DateTime(2026, 2, 10).Date);
		Assert.Equal(52m, snapshot10.MarketPricePerUnit);
		var snapshot15 = snapshots.First(s => s.Date.Date == new DateTime(2026, 2, 15).Date);
		Assert.Equal(53m, snapshot15.MarketPricePerUnit);
		// Verify dates outside range are NOT present
		Assert.DoesNotContain(snapshots, s => s.Date.Date == new DateTime(2026, 2, 5).Date);
		Assert.DoesNotContain(snapshots, s => s.Date.Date == new DateTime(2026, 2, 20).Date);
	}

	[Fact]
	public async Task GetPositionsDateRangeBoundaries()
	{
		SetupFinanceProviderMock();
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserModel user = await DataSeeder.GetTestUser(DbContext);
		
		var quote = new QuoteModel
		{
			Id = 105,
			ProviderId = "yahoo-finance",
			Symbol = "TEST6",
			Name = "Test Boundaries",
			Currency = "USD",
			ExchangeDisposition = "Test",
			TypeDisposition = "EQUITY"
		};
		DbContext.Quotes.Add(quote);
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
			Amount = 50m,
			PricePerUnit = 100m,
			TotalFees = 5m,
			Type = InvestmentType.Buy
		});
		
		var boundaryStart = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc);
		var boundaryEnd = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc);
		
		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = boundaryStart,
			Close = 105m
		});
		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = boundaryEnd,
			Close = 110m
		});
		
		await DbContext.SaveChangesAsync();

		ApiResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions(boundaryStart, boundaryEnd);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		
		var snapshots = response.Value.Snapshots.Where(s => s.QuoteId == quote.Id).ToList();
		
		Assert.Contains(snapshots, s => s.Date.Date == boundaryStart.Date);
		Assert.Contains(snapshots, s => s.Date.Date == boundaryEnd.Date);
		
		var startSnapshot = snapshots.First(s => s.Date.Date == boundaryStart.Date);
		Assert.Equal(50m, startSnapshot.Amount);
		Assert.Equal(105m, startSnapshot.MarketPricePerUnit);
		
		var endSnapshot = snapshots.First(s => s.Date.Date == boundaryEnd.Date);
		Assert.Equal(50m, endSnapshot.Amount);
		Assert.Equal(110m, endSnapshot.MarketPricePerUnit);
	}

	[Fact]
	public async Task GetPositionsComplexScenario()
	{
		SetupFinanceProviderMock();
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserModel user = await DataSeeder.GetTestUser(DbContext);
		
		var quote = new QuoteModel
		{
			Id = 106,
			ProviderId = "yahoo-finance",
			Symbol = "TEST7",
			Name = "Test Complex",
			Currency = "USD",
			ExchangeDisposition = "Test",
			TypeDisposition = "EQUITY"
		};
		DbContext.Quotes.Add(quote);
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
			Amount = 100m,
			PricePerUnit = 50m,
			TotalFees = 10m,
			Type = InvestmentType.Buy
		});
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc),
			Amount = 200m,
			PricePerUnit = 0m,
			TotalFees = 0m,
			Type = InvestmentType.Dividend
		});
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc),
			Amount = 80m,
			PricePerUnit = 55m,
			TotalFees = 8m,
			Type = InvestmentType.Buy
		});
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc),
			Amount = 120m,
			PricePerUnit = 60m,
			TotalFees = 12m,
			Type = InvestmentType.Sell
		});
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
			Amount = 150m,
			PricePerUnit = 0m,
			TotalFees = 0m,
			Type = InvestmentType.Dividend
		});
		
		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 16, 0, 0, 0, DateTimeKind.Utc),
			Close = 62m
		});
		
		await DbContext.SaveChangesAsync();

		var from = new DateTime(2026, 3, 16, 0, 0, 0, DateTimeKind.Utc);
		var to = new DateTime(2026, 3, 16, 0, 0, 0, DateTimeKind.Utc);
		ApiResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions(from, to);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		
		PositionSnapshot? snapshot = response.Value.Snapshots.FirstOrDefault(s => s.QuoteId == quote.Id);
		Assert.NotNull(snapshot);
		Assert.Equal(60m, snapshot.Amount);
		Assert.Equal(3306m, snapshot.Invested);
		Assert.Equal(9418m, snapshot.TotalInvestedCash);
		Assert.Equal(1426m, snapshot.RealizedGain);
		Assert.Equal(3720m, snapshot.CurrentValue);
		Assert.Equal(414m, snapshot.UnrealizedGain);
		Assert.Equal(1840m, snapshot.TotalProfit);
	}

	[Fact]
	public async Task GetPositionsFeeAllocation()
	{
		SetupFinanceProviderMock();
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserModel user = await DataSeeder.GetTestUser(DbContext);
		
		var quote = new QuoteModel
		{
			Id = 107,
			ProviderId = "yahoo-finance",
			Symbol = "TEST8",
			Name = "Test Fee Allocation",
			Currency = "USD",
			ExchangeDisposition = "Test",
			TypeDisposition = "EQUITY"
		};
		DbContext.Quotes.Add(quote);
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
			Amount = 100m,
			PricePerUnit = 40m,
			TotalFees = 20m,
			Type = InvestmentType.Buy
		});
		
		DbContext.Investments.Add(new InvestmentModel
		{
			UserId = user.Id,
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
			Amount = 60m,
			PricePerUnit = 50m,
			TotalFees = 15m,
			Type = InvestmentType.Sell
		});
		
		DbContext.QuotePrices.Add(new QuotePrice
		{
			QuoteId = quote.Id,
			Date = new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc),
			Close = 52m
		});
		
		await DbContext.SaveChangesAsync();

		var from = new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc);
		var to = new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc);
		ApiResponse<PositionsResponse> response = await ApiInterface.Positions.GetPositions(from, to);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value);
		
		PositionSnapshot? snapshot = response.Value.Snapshots.FirstOrDefault(s => s.QuoteId == quote.Id);
		Assert.NotNull(snapshot);
		Assert.Equal(40m, snapshot.Amount);
		// TotalFees tracks cumulative fees across all transactions (20 + 15)
		Assert.Equal(35m, snapshot.TotalFees);
		Assert.Equal(1608m, snapshot.Invested);
		Assert.Equal(573m, snapshot.RealizedGain);
	}
}
