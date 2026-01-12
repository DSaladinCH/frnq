using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.Extensions;

namespace DSaladin.Frnq.Api.Quote.Providers;

public class YahooFinanceProvider : IFinanceProvider
{
	public string InternalId => "yahoo-finance";
	public string Name => "Yahoo Finance";

	public async Task<QuoteModel?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
	{
		// https://query2.finance.yahoo.com/v1/finance/quoteType/?symbol=0P0001IFRI.SW&lang=en-US&region=US&enablePrivateCompany=true
		UriBuilder uriBuilder = new($"https://query2.finance.yahoo.com/v1/finance/quoteType/");
		QueryBuilder queryBuilder = new()
		{
			{ "symbol", symbol },
			{ "lang", "en-US" },
			{ "region", "US" },
			{ "enablePrivateCompany", "true" }
		};
		uriBuilder.Query = queryBuilder.ToString();
		string url = uriBuilder.ToString();
		HttpClient httpClient = new();
		httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
		httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
		HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);
		using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
		JsonNode? json = await JsonNode.ParseAsync(responseStream, cancellationToken: cancellationToken);
		JsonNode? resultNode = json?["quoteType"]?["result"];
		if (resultNode is null || resultNode.AsArray().Count == 0)
			return null;

		JsonNode? quoteNode = resultNode[0];

		return new QuoteModel
		{
			ProviderId = InternalId,
			Symbol = quoteNode["symbol"]?.ToString() ?? string.Empty,
			Name = QuoteModel.GetSenatizedName(quoteNode["longName"]?.ToString() ?? quoteNode["shortName"]?.ToString() ?? string.Empty),
			ExchangeDisposition = quoteNode["exchange"]?.ToString() ?? string.Empty,
			TypeDisposition = quoteNode["quoteType"]?.ToString() ?? string.Empty,
			Currency = quoteNode["currency"]?.ToString() ?? string.Empty
		};
	}

	public async Task<IEnumerable<QuoteModel>> SearchAsync(string query, CancellationToken cancellationToken = default)
	{
		UriBuilder uriBuilder = new("https://query2.finance.yahoo.com/v1/finance/search");
		QueryBuilder queryBuilder = new()
		{
			{ "q", query },
			{ "lang", "en-US" },
			{ "quotesCount", "6" },
			{ "newsCount", "0" },
			{ "listsCount", "0" },
			{ "enableFuzzyQuery", "false" }
		};
		uriBuilder.Query = queryBuilder.ToString();

		string url = uriBuilder.ToString();
		HttpClient httpClient = new();
		httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
		httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
		HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);

		using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
		JsonNode? json = await JsonNode.ParseAsync(responseStream, cancellationToken: cancellationToken);
		JsonArray? quotesArray = json?["quotes"]?.AsArray();

		if (quotesArray is null)
			return [];

		List<QuoteModel> quotes = [];

		foreach (JsonNode item in quotesArray!)
		{
			if (item is null)
				continue;

			quotes.Add(new QuoteModel
			{
				ProviderId = InternalId,
				Symbol = item["symbol"]?.ToString() ?? string.Empty,
				Name = QuoteModel.GetSenatizedName(item["longname"]?.ToString() ?? item["shortname"]?.ToString() ?? string.Empty),
				ExchangeDisposition = item["exchDisp"]?.ToString() ?? string.Empty,
				TypeDisposition = item["typeDisp"]?.ToString() ?? string.Empty
			});
		}

		return quotes;
	}

	public async Task<IEnumerable<QuotePrice>> GetHistoricalPricesAsync(string symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default)
	{
		from = DateTime.SpecifyKind(from, DateTimeKind.Utc);
		to = DateTime.SpecifyKind(to, DateTimeKind.Utc);

		if (from == DateTime.MinValue)
			from = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		UriBuilder uriBuilder = new($"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}");
		QueryBuilder queryBuilder = new()
		{
			{ "formatted", "true" },
			{ "includeAdjustedClose", "true" },
			{ "interval", "1d" },
			{ "period1", ((DateTimeOffset)from).ToUnixTimeSeconds().ToString() },
			{ "period2", ((DateTimeOffset)to).ToUnixTimeSeconds().ToString() },
			{ "userYfid", "true" },
			{ "lang", "en-US" },
			{ "region", "US" }
		};

		uriBuilder.Query = queryBuilder.ToString();
		string url = uriBuilder.ToString();
		HttpClient httpClient = new();
		httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
		httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
		HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);
		using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
		JsonNode? json = await JsonNode.ParseAsync(responseStream, cancellationToken: cancellationToken);

		if (json is null)
			return [];

		JsonNode? currency = json["chart"]?["result"]?[0]?["meta"]?["currency"];
		JsonArray? timestamps = json["chart"]?["result"]?[0]?["timestamp"]?.AsArray();
		JsonNode? quoteNode = json["chart"]?["result"]?[0]?["indicators"]?["quote"]?[0];
		JsonNode? adjCloseNode = json["chart"]?["result"]?[0]?["indicators"]?["adjclose"]?[0]?["adjclose"];

		JsonArray? opens = quoteNode?["open"]?.AsArray();
		JsonArray? closes = quoteNode?["close"]?.AsArray();
		JsonArray? highs = quoteNode?["high"]?.AsArray();
		JsonArray? lows = quoteNode?["low"]?.AsArray();
		JsonArray? adjCloses = adjCloseNode?.AsArray();

		if (timestamps is null || opens is null || closes is null || highs is null || lows is null || adjCloses is null)
			return [];

		List<QuotePrice> result = [];

		for (int i = 0; i < opens.Count; i++)
		{
			if (timestamps[i] is null || opens[i] is null || closes[i] is null || highs[i] is null || lows[i] is null || adjCloses[i] is null)
				continue;

			result.Add(new QuotePrice
			{
				Date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]!.GetValue<long>()).UtcDateTime,
				Open = opens[i]!.GetValue<decimal>(),
				Close = closes[i]!.GetValue<decimal>(),
				High = highs[i]!.GetValue<decimal>(),
				Low = lows[i]!.GetValue<decimal>(),
				AdjustedClose = adjCloses[i]!.GetValue<decimal>(),
				Currency = currency!.GetValue<string>()
			});
		}

		return result;
	}
}