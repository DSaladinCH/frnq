using System.Net.Http.Json;
using DSaladin.Frnq.Api.Group;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public class GroupApi(HttpClient httpClient) : BaseApi(httpClient)
{
    public async Task<TestResponse<List<QuoteGroupViewDto>>> GetGroups()
        => await GetAsync<List<QuoteGroupViewDto>>("api/groups");

    public async Task<TestResponse> CreateGroup(QuoteGroupDto quoteGroup)
        => await PostAsync("api/groups", quoteGroup);

    public async Task<TestResponse> UpdateGroup(int id, QuoteGroupDto quoteGroup)
        => await PutAsync($"api/groups/{id}", quoteGroup);

    public async Task<TestResponse> DeleteGroup(int id)
        => await DeleteAsync($"api/groups/{id}");

    public async Task<TestResponse> AddQuoteToGroup(int id, int quoteId)
        => await PostAsync($"api/groups/{id}/{quoteId}");

    public async Task<TestResponse> RemoveQuoteFromGroup(int id, int quoteId)
        => await DeleteAsync($"api/groups/{id}/{quoteId}");
}
