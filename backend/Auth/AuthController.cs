using DSaladin.Frnq.Api.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthManagement authManagement, IConfiguration configuration) : ControllerBase
{
    [HttpGet("signup-enabled")]
    public IActionResult GetSignupEnabled()
    {
        bool signupEnabled = configuration.GetValue<bool>("Features:SignupEnabled", true);
        return Ok(new { signupEnabled });
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupModel signup)
    {
        return await authManagement.SignupUserAsync(signup);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel login)
    {
        return await authManagement.LoginUserAsync(login);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        return await authManagement.RefreshAccessTokenAsync();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        return await authManagement.LogoutAsync();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
		return await authManagement.GetUserAsync();
    }

    [HttpPatch("me")]
    [Authorize]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserModel updateModel)
    {
        return await authManagement.UpdateUserAsync(updateModel);
    }
}