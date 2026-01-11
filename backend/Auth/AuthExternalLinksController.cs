using DSaladin.Frnq.Api.Result;
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
    public async Task<ApiResponse> GetLinks(CancellationToken cancellationToken)
    {
		Guid userId = authManagement.GetCurrentUserId();
        if (userId == Guid.Empty)
            return ApiResponses.Unauthorized401;

        return await oidcManagement.GetUserLinksAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Initiate linking of an external OIDC account
    /// Returns the authorization URL for the frontend to redirect to
    /// </summary>
    [HttpPost("link/{providerId}")]
    public async Task<ApiResponse> InitiateLink(string providerId, CancellationToken cancellationToken)
    {
		Guid userId = authManagement.GetCurrentUserId();
        if (userId == Guid.Empty)
            return ApiResponses.Unauthorized401;

		// Pass userId to the OIDC flow for linking
		string returnUrl = $"/settings/external-accounts";
		ApiResponse<string> result = await oidcManagement.InitiateLoginAsync(providerId, returnUrl, userId, cancellationToken);
        
        if (!result.Success || result.Value == null)
            return result;

        // Return the authorization URL for the frontend to redirect to
        return ApiResponse.Create(new { authorizationUrl = result.Value }, System.Net.HttpStatusCode.OK);
    }

    /// <summary>
    /// Unlink an external account
    /// </summary>
    [HttpDelete("{linkId}")]
    public async Task<ApiResponse> UnlinkAccount(Guid linkId, CancellationToken cancellationToken)
    {
		Guid userId = authManagement.GetCurrentUserId();
        if (userId == Guid.Empty)
            return ApiResponses.Unauthorized401;

        return await oidcManagement.UnlinkExternalAccountAsync(userId, linkId, cancellationToken);
    }
}
