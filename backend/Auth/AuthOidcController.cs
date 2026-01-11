using DSaladin.Frnq.Api.Result;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Auth;

/// <summary>
/// Controller for OIDC authentication endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthOidcController(OidcManagement oidcManagement, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Get list of enabled OIDC providers
    /// </summary>
    [HttpGet("providers")]
    public async Task<ApiResponse> GetProviders(CancellationToken cancellationToken)
    {
        return await oidcManagement.GetEnabledProvidersAsync(cancellationToken);
    }

    /// <summary>
    /// Initiate OIDC login flow
    /// Redirects user to the external provider's authorization endpoint
    /// </summary>
    /// <param name="providerId">The provider identifier (e.g., "google", "azure")</param>
    /// <param name="returnUrl">Optional return URL after successful authentication</param>
    [HttpGet("login/{providerId}")]
    public async Task<IActionResult> InitiateLogin(string providerId, [FromQuery] string? returnUrl = null, CancellationToken cancellationToken = default)
    {
		ApiResponse<string> result = await oidcManagement.InitiateLoginAsync(providerId, returnUrl, null, cancellationToken);
        
        if (result.Failed || result.Value == null)
            return result;

        // Redirect to the authorization URL
        return Redirect(result.Value);
    }

    /// <summary>
    /// Callback endpoint that receives the authorization code from the OIDC provider
    /// </summary>
    /// <param name="providerId">The provider identifier</param>
    /// <param name="code">Authorization code from the provider</param>
    /// <param name="state">State parameter for CSRF protection</param>
    [HttpGet("callback/{providerId}")]
    public async Task<IActionResult> Callback(
        string providerId,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery] string? error_description,
		CancellationToken cancellationToken = default)
    {
		string frontendUrl = configuration.GetValue<string>("FrontendUrl") ?? "http://localhost:5173";

        // Handle OAuth errors
        if (!string.IsNullOrEmpty(error))
        {
			string errorMessage = Uri.EscapeDataString(error_description ?? error);
            return Redirect($"{frontendUrl}/login?error={errorMessage}");
        }

        // Validate required parameters
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return Redirect($"{frontendUrl}/login?error=invalid_callback");
        }

		// Process the callback
		ApiResponse<LoginResponseModel> result = await oidcManagement.HandleCallbackAsync(providerId, code, state, cancellationToken);
        
        if (result.Code == "LINK_SUCCESS")
        {
            // Account linked successfully - redirect to settings
            return Redirect($"{frontendUrl}/settings?oidc_linked=true");
        }
        else if (result.Success && result.Value != null)
        {
            // Successful authentication - redirect to frontend with success
            // The refresh token cookie is already set by AuthManagement
            return Redirect($"{frontendUrl}/portfolio");
        }
        else if (result.Code == "NOT_LINKED")
        {
			// Not linked - redirect to login with message
			string errorMsg = Uri.EscapeDataString("This external account is not linked. Please log in and link it in your account settings.");
            return Redirect($"{frontendUrl}/login?error={errorMsg}");
        }
        else
        {
			// Failed authentication - redirect to login with error
			string errorMsg = Uri.EscapeDataString(result.Description ?? "Authentication failed");
            return Redirect($"{frontendUrl}/login?error={errorMsg}");
        }
    }
}
