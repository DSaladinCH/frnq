using System.Net.Http.Json;
using System.Text.Json;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public class AuthApi(HttpClient httpClient) : BaseApi(httpClient)
{
    public async Task<TestResponse<bool>> GetSignupEnabled()
        => await GetAsync<bool>("api/auth/signup-enabled");

    public async Task<TestResponse> Signup(SignupDto signup)
        => await PostAsync("api/auth/signup", signup);

    public async Task<TestResponse<AuthResponseDto>> Login(LoginDto login)
        => await PostAsync<AuthResponseDto, LoginDto>("api/auth/login", login);

    public async Task<TestResponse<AuthResponseDto>> RefreshToken()
        => await PostAsync<AuthResponseDto, object?>("api/auth/refresh", null);

    public async Task<TestResponse> Logout()
        => await PostAsync("api/auth/logout");

    public async Task<TestResponse<UserViewDto>> GetCurrentUser()
        => await GetAsync<UserViewDto>("api/auth/me");

    public async Task<TestResponse> UpdateCurrentUser(UserDto updateModel)
    {
        var response = await HttpClient.PatchAsJsonAsync("api/auth/me", updateModel);
        var testResponse = new TestResponse { StatusCode = response.StatusCode };
        var content = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(content))
            testResponse.Error = JsonSerializer.Deserialize<CodeDescriptionModel>(content, JsonOptions);
        return testResponse;
    }
}
