using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Auth;

/// <summary>
/// Controller for managing external account links
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthExternalLinksController(
    OidcManagement oidcManagement,
    AuthManagement authManagement) : ControllerBase
{
    /// <summary>
    /// Get all external account links for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLinks()
    {
        var userId = authManagement.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var links = await oidcManagement.GetUserLinksAsync(userId);
        return Ok(links);
    }

    /// <summary>
    /// Initiate linking of an external OIDC account
    /// Returns the authorization URL for the frontend to redirect to
    /// </summary>
    [HttpPost("link/{providerId}")]
    public async Task<IActionResult> InitiateLink(string providerId)
    {
        var userId = authManagement.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        // Pass userId to the OIDC flow for linking
        var returnUrl = $"/settings/external-accounts";
        var result = await oidcManagement.InitiateLoginAsync(providerId, returnUrl, userId);
        
        if (!result.Success || result.Value == null)
            return result;

        // Return the authorization URL for the frontend to redirect to
        return Ok(new { authorizationUrl = result.Value });
    }

    /// <summary>
    /// Unlink an external account
    /// </summary>
    [HttpDelete("{linkId}")]
    public async Task<IActionResult> UnlinkAccount(Guid linkId)
    {
        var userId = authManagement.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await oidcManagement.UnlinkExternalAccountAsync(userId, linkId);
        return result;
    }
}
