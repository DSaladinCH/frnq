using System.Net.Http.Json;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public class QuoteApi(HttpClient httpClient) : BaseApi(httpClient)
{
    public async Task<ApiResponse<List<QuoteModel>>> Search(string query, string providerId = "yahoo-finance")
        => await GetAsync<List<QuoteModel>>($"api/quote/search?query={query}&providerId={providerId}");

    public async Task<ApiResponse<List<QuotePrice>>> GetHistoricalPrices(string symbol, DateTime from, DateTime to, string providerId = "yahoo-finance")
        => await GetAsync<List<QuotePrice>>($"api/quote/historical?symbol={symbol}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&providerId={providerId}");

    public async Task<ApiResponse> UpdateCustomName(int quoteId, CustomNameDto customName)
        => await PutAsync($"api/quote/{quoteId}/customName", customName);

    public async Task<ApiResponse> DeleteCustomName(int quoteId)
        => await DeleteAsync($"api/quote/{quoteId}/customName");
}
