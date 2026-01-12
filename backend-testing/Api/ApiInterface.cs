using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

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

	protected async Task<ApiResponse<T>> GetAsync<T>(string url)
	{
		HttpResponseMessage response = await HttpClient.GetAsync(url);
		return await ParseResponse<T>(response);
	}

	protected async Task<ApiResponse> GetAsync(string url)
	{
		HttpResponseMessage response = await HttpClient.GetAsync(url);
		return await ParseResponse(response);
	}

	protected async Task<ApiResponse<T>> PostAsync<T, TRequest>(string url, TRequest request)
	{
		HttpResponseMessage response = await HttpClient.PostAsJsonAsync(url, request);
		return await ParseResponse<T>(response);
	}

	protected async Task<ApiResponse> PostAsync<TRequest>(string url, TRequest request)
	{
		HttpResponseMessage response = await HttpClient.PostAsJsonAsync(url, request);
		return await ParseResponse(response);
	}

	protected async Task<ApiResponse> PostAsync(string url)
	{
		HttpResponseMessage response = await HttpClient.PostAsync(url, null);
		return await ParseResponse(response);
	}

	protected async Task<ApiResponse> PutAsync<TRequest>(string url, TRequest request)
	{
		HttpResponseMessage response = await HttpClient.PutAsJsonAsync(url, request);
		return await ParseResponse(response);
	}

	protected async Task<ApiResponse> DeleteAsync(string url)
	{
		HttpResponseMessage response = await HttpClient.DeleteAsync(url);
		return await ParseResponse(response);
	}

	private static async Task<ApiResponse<T>> ParseResponse<T>(HttpResponseMessage response)
		=> await response.ParseApiResponse<T>();

	private static async Task<ApiResponse> ParseResponse(HttpResponseMessage response)
		=> await response.ParseApiResponse();
}

