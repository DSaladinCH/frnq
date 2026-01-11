using DSaladin.Frnq.Api.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthManagement authManagement, IConfiguration configuration) : ControllerBase
{
    [HttpGet("signup-enabled")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public ApiResponse GetSignupEnabled()
    {
        return ApiResponse.Create(configuration.GetValue("Features:SignupEnabled", false), System.Net.HttpStatusCode.OK);
    }

    [HttpPost("signup")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status409Conflict)]
    public async Task<ApiResponse> Signup([FromBody] SignupModel signup, CancellationToken cancellationToken)
    {
        return await authManagement.SignupUserAsync(signup, cancellationToken);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse> Login([FromBody] LoginModel login, CancellationToken cancellationToken)
    {
        return await authManagement.LoginUserAsync(login, cancellationToken);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse> RefreshToken(CancellationToken cancellationToken)
    {
        return await authManagement.RefreshAccessTokenAsync(cancellationToken);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ApiResponse> Logout(CancellationToken cancellationToken)
    {
        return await authManagement.LogoutAsync(cancellationToken);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status401Unauthorized)]
    public async Task<ApiResponse> GetCurrentUser(CancellationToken cancellationToken)
    {
		return await authManagement.GetUserAsync(cancellationToken);
    }

    [HttpPatch("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse> UpdateCurrentUser([FromBody] UserDto updateModel, CancellationToken cancellationToken)
    {
        return await authManagement.UpdateUserAsync(updateModel, cancellationToken);
    }
}