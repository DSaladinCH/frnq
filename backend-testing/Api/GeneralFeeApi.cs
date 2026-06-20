using DSaladin.Frnq.Api.GeneralFee;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public class GeneralFeeApi(HttpClient httpClient) : BaseApi(httpClient)
{
	public async Task<ApiResponse<PaginatedGeneralFeesResponse>> GetFees(int skip = 0, int take = 25, int? groupId = null)
	{
		var parameters = new List<string> { $"skip={skip}", $"take={take}" };
		if (groupId.HasValue) parameters.Add($"groupId={groupId.Value}");
		return await GetAsync<PaginatedGeneralFeesResponse>($"api/general-fees?{string.Join("&", parameters)}");
	}

	public async Task<ApiResponse<GeneralFeeViewDto>> CreateFee(GeneralFeeDto request)
		=> await PostAsync<GeneralFeeViewDto, GeneralFeeDto>("api/general-fees", request);

	public async Task<ApiResponse<GeneralFeeViewDto>> UpdateFee(int id, GeneralFeeDto request)
		=> await PutAsync<GeneralFeeViewDto, GeneralFeeDto>($"api/general-fees/{id}", request);

	public async Task<ApiResponse> DeleteFee(int id)
		=> await DeleteAsync($"api/general-fees/{id}");
}
