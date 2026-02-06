using System.Net.Http.Json;
using System.Text.Json;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Api;

public class AuthApi(HttpClient httpClient) : BaseApi(httpClient)
{
    public async Task<ApiResponse<bool>> GetSignupEnabled()
        => await GetAsync<bool>("api/auth/signup-enabled");

    public async Task<ApiResponse> Signup(SignupDto signup)
        => await PostAsync("api/auth/signup", signup);

    public async Task<ApiResponse<AuthResponseDto>> Login(LoginDto login)
        => await PostAsync<AuthResponseDto, LoginDto>("api/auth/login", login);

    public async Task<ApiResponse<AuthResponseDto>> RefreshToken()
        => await PostAsync<AuthResponseDto, object?>("api/auth/refresh", null);

    public async Task<ApiResponse> Logout()
        => await PostAsync("api/auth/logout");

    public async Task<ApiResponse<UserViewDto>> GetCurrentUser()
        => await GetAsync<UserViewDto>("api/auth/me");

    public async Task<ApiResponse> UpdateCurrentUser(UserDto updateModel)
		=> await PutAsync("api/auth/me", updateModel);
}
