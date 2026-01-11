using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public class AuthExternalLinksApi(HttpClient httpClient) : BaseApi(httpClient)
{
    public async Task<TestResponse<List<ExternalLinkViewDto>>> GetLinks()
        => await GetAsync<List<ExternalLinkViewDto>>("api/AuthExternalLinks");

    public async Task<TestResponse<object>> InitiateLink(string providerId)
        => await PostAsync<object, object?>($"api/AuthExternalLinks/link/{providerId}", null);

    public async Task<TestResponse> UnlinkAccount(Guid linkId)
        => await DeleteAsync($"api/AuthExternalLinks/{linkId}");
}

public class AuthOidcApi(HttpClient httpClient) : BaseApi(httpClient)
{
    public async Task<TestResponse<List<OidcProviderDto>>> GetProviders()
        => await GetAsync<List<OidcProviderDto>>("api/AuthOidc/providers");

    public async Task<TestResponse<object>> InitiateLogin(string providerId)
        => await GetAsync<object>($"api/AuthOidc/login/{providerId}");

    // Callback is tricky
}
