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
    public async Task<ApiResponse> Signup([FromBody] SignupModel signup, CancellationToken cancellationToken)
    {
        return await authManagement.SignupUserAsync(signup, cancellationToken);
    }

    [HttpPost("login")]
    public async Task<ApiResponse> Login([FromBody] LoginModel login, CancellationToken cancellationToken)
    {
        return await authManagement.LoginUserAsync(login, cancellationToken);
    }

    [HttpPost("refresh")]
    public async Task<ApiResponse> RefreshToken(CancellationToken cancellationToken)
    {
        return await authManagement.RefreshAccessTokenAsync(cancellationToken);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ApiResponse> Logout(CancellationToken cancellationToken)
    {
        return await authManagement.LogoutAsync(cancellationToken);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ApiResponse> GetCurrentUser(CancellationToken cancellationToken)
    {
		return await authManagement.GetUserAsync(cancellationToken);
    }

    [HttpPatch("me")]
    [Authorize]
    public async Task<ApiResponse> UpdateCurrentUser([FromBody] UserDto updateModel, CancellationToken cancellationToken)
    {
        return await authManagement.UpdateUserAsync(updateModel, cancellationToken);
    }
}