using DSaladin.Frnq.Api.Forecast;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public class ForecastApi(HttpClient httpClient) : BaseApi(httpClient)
{
	public async Task<ApiResponse<List<ForecastDayDto>>> GetForecast(int years = 10, bool includeContributions = false)
		=> await GetAsync<List<ForecastDayDto>>($"api/forecast?years={years}&includeContributions={includeContributions}");
}
