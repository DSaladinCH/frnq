using System.Net.Http.Json;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public class InvestmentApi(HttpClient httpClient) : BaseApi(httpClient)
{
    public async Task<ApiResponse<PaginatedInvestmentsResponse>> GetInvestments(int skip = 0, int take = 25)
        => await GetAsync<PaginatedInvestmentsResponse>($"api/investments?skip={skip}&take={take}");

    public async Task<ApiResponse<InvestmentViewDto>> GetInvestmentById(int id)
        => await GetAsync<InvestmentViewDto>($"api/investments/{id}");

    public async Task<ApiResponse> CreateInvestment(InvestmentDto investment)
        => await PostAsync("api/investments", investment);

    public async Task<ApiResponse> CreateInvestmentsBulk(List<InvestmentDto> investments)
        => await PostAsync("api/investments/bulk", investments);

    public async Task<ApiResponse> UpdateInvestment(int id, InvestmentDto investment)
        => await PutAsync($"api/investments/{id}", investment);

    public async Task<ApiResponse> DeleteInvestment(int id)
        => await DeleteAsync($"api/investments/{id}");
}
