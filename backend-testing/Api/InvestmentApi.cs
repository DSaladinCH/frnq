using System.Net.Http.Json;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public class InvestmentApi(HttpClient httpClient) : BaseApi(httpClient)
{
    public async Task<ApiResponse<PaginatedInvestmentsResponse>> GetInvestments(int skip, int take, DateTime? fromDate = null, DateTime? toDate = null, int? quoteId = null, int? groupId = null, InvestmentType? type = null)
	{
		var parameters = new List<string>
		{
			$"skip={skip}",
			$"take={take}"
		};
		
		if (fromDate.HasValue)
			parameters.Add($"fromDate={fromDate.Value:O}");
		
		if (toDate.HasValue)
			parameters.Add($"toDate={toDate.Value:O}");

		if (quoteId.HasValue)
			parameters.Add($"quoteId={quoteId.Value}");

		if (groupId.HasValue)
			parameters.Add($"groupId={groupId.Value}");

		if (type.HasValue)
			parameters.Add($"type={type.Value}");

		string queryString = string.Join("&", parameters);
        return await GetAsync<PaginatedInvestmentsResponse>($"api/investments?{queryString}");
	}

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
