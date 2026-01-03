using DSaladin.Frnq.Api.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthManagement authManagement, IConfiguration configuration) : ControllerBase
{
    [HttpGet("signup-enabled")]
    public ApiResponse GetSignupEnabled()
    {
        return ApiResponse.Create(configuration.GetValue("Features:SignupEnabled", false), System.Net.HttpStatusCode.OK);
    }

    [HttpPost("signup")]
    public async Task<ApiResponse> Signup([FromBody] SignupModel signup)
    {
        return await authManagement.SignupUserAsync(signup);
    }

    [HttpPost("login")]
    public async Task<ApiResponse> Login([FromBody] LoginModel login)
    {
        return await authManagement.LoginUserAsync(login);
    }

    [HttpPost("refresh")]
    public async Task<ApiResponse> RefreshToken()
    {
        return await authManagement.RefreshAccessTokenAsync();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ApiResponse> Logout()
    {
        return await authManagement.LogoutAsync();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ApiResponse> GetCurrentUser()
    {
		return await authManagement.GetUserAsync();
    }

    [HttpPatch("me")]
    [Authorize]
    public async Task<ApiResponse> UpdateCurrentUser([FromBody] UpdateUserModel updateModel)
    {
        return await authManagement.UpdateUserAsync(updateModel);
    }
}