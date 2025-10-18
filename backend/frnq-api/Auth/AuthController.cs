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
    public IActionResult GetCurrentUser()
    {
        // This endpoint demonstrates how to use [Authorize] and access user claims
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        return Ok(new 
        { 
            userId = userId,
            email = email,
            name = name,
            message = "This endpoint requires valid JWT authentication"
        });
    }
}