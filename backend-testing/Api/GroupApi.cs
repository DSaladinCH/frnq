using System.Net.Http.Json;
using DSaladin.Frnq.Api.Group;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public class GroupApi(HttpClient httpClient) : BaseApi(httpClient)
{
    public async Task<ApiResponse<List<QuoteGroupViewDto>>> GetGroups()
		=> await GetAsync<List<QuoteGroupViewDto>>("api/groups");

    public async Task<ApiResponse> CreateGroup(QuoteGroupDto quoteGroup)
        => await PostAsync("api/groups", quoteGroup);

    public async Task<ApiResponse> UpdateGroup(int id, QuoteGroupDto quoteGroup)
        => await PutAsync($"api/groups/{id}", quoteGroup);

    public async Task<ApiResponse> DeleteGroup(int id)
        => await DeleteAsync($"api/groups/{id}");

    public async Task<ApiResponse> AddQuoteToGroup(int id, int quoteId)
        => await PostAsync($"api/groups/{id}/{quoteId}");

    public async Task<ApiResponse> RemoveQuoteFromGroup(int id, int quoteId)
        => await DeleteAsync($"api/groups/{id}/{quoteId}");
}
