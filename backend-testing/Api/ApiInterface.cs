using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public class TestResponse<T>
{
    public HttpStatusCode StatusCode { get; set; }
    public T? Content { get; set; }
    public CodeDescriptionModel? Error { get; set; }
    public System.Net.Http.Headers.HttpResponseHeaders? Headers { get; set; }
    public bool Success => (int)StatusCode >= 200 && (int)StatusCode < 300;
}

public class TestResponse
{
    public HttpStatusCode StatusCode { get; set; }
    public CodeDescriptionModel? Error { get; set; }
    public System.Net.Http.Headers.HttpResponseHeaders? Headers { get; set; }
    public bool Success => (int)StatusCode >= 200 && (int)StatusCode < 300;
}

public class ApiInterface(HttpClient httpClient)
{
    private string? _token;

    public void SetToken(string token)
    {
        _token = token;
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearToken()
    {
        _token = null;
        httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public AuthApi Auth => new(httpClient);
    public InvestmentApi Investments => new(httpClient);
    public GroupApi Groups => new(httpClient);
    public PositionApi Positions => new(httpClient);
    public QuoteApi Quotes => new(httpClient);
    public AuthExternalLinksApi ExternalLinks => new(httpClient);
    public AuthOidcApi Oidc => new(httpClient);
}

public abstract class BaseApi(HttpClient httpClient)
{
    protected readonly HttpClient HttpClient = httpClient;
    protected readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    protected async Task<TestResponse<T>> GetAsync<T>(string url)
    {
        var response = await HttpClient.GetAsync(url);
        return await ParseResponse<T>(response);
    }

    protected async Task<TestResponse> GetAsync(string url)
    {
        var response = await HttpClient.GetAsync(url);
        return await ParseResponse(response);
    }

    protected async Task<TestResponse<T>> PostAsync<T, TRequest>(string url, TRequest request)
    {
        var response = await HttpClient.PostAsJsonAsync(url, request);
        return await ParseResponse<T>(response);
    }

    protected async Task<TestResponse> PostAsync<TRequest>(string url, TRequest request)
    {
        var response = await HttpClient.PostAsJsonAsync(url, request);
        return await ParseResponse(response);
    }

    protected async Task<TestResponse> PostAsync(string url)
    {
        var response = await HttpClient.PostAsync(url, null);
        return await ParseResponse(response);
    }

    protected async Task<TestResponse> PutAsync<TRequest>(string url, TRequest request)
    {
        var response = await HttpClient.PutAsJsonAsync(url, request);
        return await ParseResponse(response);
    }

    protected async Task<TestResponse> DeleteAsync(string url)
    {
        var response = await HttpClient.DeleteAsync(url);
        return await ParseResponse(response);
    }

    private async Task<TestResponse<T>> ParseResponse<T>(HttpResponseMessage response)
    {
        var testResponse = new TestResponse<T> { StatusCode = response.StatusCode, Headers = response.Headers };
        
        if (response.StatusCode == HttpStatusCode.NoContent)
            return testResponse;

        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            if (!string.IsNullOrWhiteSpace(content))
                testResponse.Content = JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                try {
                    testResponse.Error = JsonSerializer.Deserialize<CodeDescriptionModel>(content, JsonOptions);
                } catch { /* Ignored */ }
            }
        }

        return testResponse;
    }

    private async Task<TestResponse> ParseResponse(HttpResponseMessage response)
    {
        var testResponse = new TestResponse { StatusCode = response.StatusCode, Headers = response.Headers };

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(content))
            {
                try {
                    testResponse.Error = JsonSerializer.Deserialize<CodeDescriptionModel>(content, JsonOptions);
                } catch { /* Ignored */ }
            }
        }

        return testResponse;
    }
}

