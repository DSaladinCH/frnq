using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public static class HttpResponseExtension
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Parses the HttpResponseMessage into an ApiResponse of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the data expected in the API response.</typeparam>
    /// <param name="response">The HTTP response message to parse.</param>
    /// <returns>An ApiResponse containing the parsed data and status code.</returns>
    public static async Task<ApiResponse<T>> ParseApiResponse<T>(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            ApiResponse<T> responseContent = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions) ?? throw new Exception("Failed to deserialize API response");
            responseContent.StatusCode = response.StatusCode;
            return responseContent;
        }
        T? apiResponse = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return ApiResponse.Create(apiResponse, response.StatusCode);
    }

    /// <summary>
    /// Parses the HttpResponseMessage into a non-generic ApiResponse.
    /// </summary>
    /// <param name="response">The HTTP response message to parse.</param>
    /// <returns>An ApiResponse containing the status code.</returns>
    public static async Task<ApiResponse> ParseApiResponse(this HttpResponseMessage response)
    {
        if (response.StatusCode is System.Net.HttpStatusCode.NoContent or System.Net.HttpStatusCode.Created)
            return ApiResponse.Create((string?)null, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>(JsonOptions) ?? throw new Exception("Failed to deserialize API response");
        apiResponse.StatusCode = response.StatusCode;
        return apiResponse;
    }
}
