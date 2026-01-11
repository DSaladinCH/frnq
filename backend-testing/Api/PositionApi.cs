using System.Net.Http.Json;
using DSaladin.Frnq.Api.Position;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public class PositionApi(HttpClient httpClient) : BaseApi(httpClient)
{
    public async Task<TestResponse<PositionsResponse>> GetPositions(DateTime? from = null, DateTime? to = null)
    {
        var url = "api/positions";
        if (from.HasValue || to.HasValue)
        {
            url += "?";
            if (from.HasValue) url += $"from={from.Value:yyyy-MM-dd}";
            if (from.HasValue && to.HasValue) url += "&";
            if (to.HasValue) url += $"to={to.Value:yyyy-MM-dd}";
        }
        return await GetAsync<PositionsResponse>(url);
    }
}
