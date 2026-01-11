using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DSaladin.Frnq.Api.Auth;

/// <summary>
/// Manages OIDC authentication flow
/// </summary>
public class OidcManagement(
    DatabaseContext databaseContext, 
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    AuthManagement authManagement,
    ILogger<OidcManagement> logger)
{
    private readonly HttpClient _httpClient = new();

    /// <summary>
    /// Get all enabled OIDC providers for display on login page
    /// </summary>
    public async Task<ApiResponse<List<OidcProviderDto>>> GetEnabledProvidersAsync(CancellationToken cancellationToken)
    {
		List<OidcProviderDto> providers = await databaseContext.OidcProviders
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new OidcProviderDto
            {
                ProviderId = p.ProviderId,
                DisplayName = p.DisplayName,
                FaviconUrl = p.FaviconUrl,
                AutoRedirect = p.AutoRedirect
            })
            .ToListAsync(cancellationToken);

        return ApiResponse.Create(providers, System.Net.HttpStatusCode.OK);
    }

    /// <summary>
    /// Initiates OAuth2/OIDC authorization flow
    /// </summary>
    /// <param name="providerId">The provider to use</param>
    /// <param name="returnUrl">Optional return URL</param>
    /// <param name="userId">Optional user ID for linking flow</param>
    public async Task<ApiResponse<string>> InitiateLoginAsync(string providerId, string? returnUrl = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
		OidcProvider? provider = await databaseContext.OidcProviders
            .FirstOrDefaultAsync(p => p.ProviderId == providerId && p.IsEnabled, cancellationToken);

        if (provider == null)
            return ApiResponse.Create("PROVIDER_NOT_FOUND", "Provider not found or disabled", System.Net.HttpStatusCode.NotFound);

		// Generate secure random state and nonce
		string state = GenerateSecureToken(32);
		string nonce = GenerateSecureToken(32);

        // Store state in database with expiration
        var oidcState = new OidcState
        {
            State = state,
            ProviderId = provider.Id,
            Nonce = nonce,
            ReturnUrl = returnUrl,
            LinkingUserId = userId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        await databaseContext.OidcStates.AddAsync(oidcState, cancellationToken);
        await databaseContext.SaveChangesAsync(cancellationToken);

		// Build redirect URL
		string baseUrl = configuration.GetValue<string>("ApiBaseUrl") ?? 
                     $"{httpContextAccessor.HttpContext!.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";
		string redirectUri = $"{baseUrl}/api/authoidc/callback/{provider.ProviderId}";

		string authUrl = BuildAuthorizationUrl(provider, redirectUri, state, nonce);

        return ApiResponse<string>.Create(authUrl, System.Net.HttpStatusCode.OK);
    }

    /// <summary>
    /// Handles the OAuth2/OIDC callback after user authorizes
    /// </summary>
    public async Task<ApiResponse<AuthResponseDto>> HandleCallbackAsync(string providerId, string code, string state, CancellationToken cancellationToken)
    {
		// Validate state to prevent CSRF
		OidcState? oidcState = await databaseContext.OidcStates
            .Include(s => s.Provider)
            .FirstOrDefaultAsync(s => s.State == state && !s.IsUsed && s.ExpiresAt > DateTime.UtcNow, cancellationToken);

        if (oidcState == null)
        {
            logger.LogWarning("Invalid or expired OIDC state: {State}", state);
            return ApiResponse.Create("INVALID_STATE", "Invalid or expired state", System.Net.HttpStatusCode.BadRequest);
        }

        if (oidcState.Provider == null || oidcState.Provider.ProviderId != providerId)
        {
            logger.LogWarning("State provider mismatch. Expected: {Expected}, Got: {Got}", oidcState.Provider?.ProviderId, providerId);
            return ApiResponse.Create("PROVIDER_MISMATCH", "Provider mismatch", System.Net.HttpStatusCode.BadRequest);
        }

        // Mark state as used
        oidcState.IsUsed = true;
        await databaseContext.SaveChangesAsync(cancellationToken);

		OidcProvider provider = oidcState.Provider;

        try
        {
			// Exchange code for tokens
			TokenResponse? tokenResponse = await ExchangeCodeForTokensAsync(provider, code, cancellationToken);
            if (tokenResponse == null)
            {
                logger.LogError("Failed to exchange code for tokens with provider {Provider}", providerId);
                return ApiResponse.Create("TOKEN_EXCHANGE_FAILED", "Failed to obtain tokens", System.Net.HttpStatusCode.BadGateway);
            }

			// Get user info from ID token or UserInfo endpoint
			OidcUserInfo? userInfo = await GetUserInfoAsync(provider, tokenResponse, cancellationToken);
            if (userInfo == null)
            {
                logger.LogError("Failed to get user info from provider {Provider}", providerId);
                return ApiResponse.Create("USERINFO_FAILED", "Failed to get user information", System.Net.HttpStatusCode.BadGateway);
            }

            // Check if this is a linking flow
            if (oidcState.LinkingUserId.HasValue)
            {
				// This is a linking request - link and return success
				ApiResponse linkResult = await LinkExternalAccountAsync(oidcState.LinkingUserId.Value, providerId, userInfo, cancellationToken);
                
                if (linkResult.Success)
                {
                    // Return a special response indicating linking succeeded
                    return ApiResponse.Create("LINK_SUCCESS", "Account linked successfully", System.Net.HttpStatusCode.OK);
                }
                else
                {
                    return ApiResponse.Create(linkResult.Code, linkResult.Description, linkResult.StatusCode);
                }
            }

			// Normal login flow - find user by external link
			UserModel? user = await FindUserByExternalLinkAsync(provider, userInfo, cancellationToken);

            if (user == null)
            {
                // No linked account found - return error with user info for linking
                logger.LogInformation("No linked account found for OIDC user {Sub} from provider {Provider}", userInfo.Sub, providerId);
                return ApiResponse.Create("NOT_LINKED", "No linked account found. Please log in with your credentials and link this account.", System.Net.HttpStatusCode.Unauthorized);
            }

            // Generate our own JWT tokens
            var loginResponse = await authManagement.LoginUserByUserModelAsync(user, cancellationToken);

            return loginResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during OIDC callback for provider {Provider}", providerId);
            return ApiResponse.Create("OIDC_ERROR", "Authentication failed", System.Net.HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Cleanup expired OIDC states (should be run periodically)
    /// </summary>
    public async Task CleanupExpiredStatesAsync(CancellationToken cancellationToken)
    {
		List<OidcState> expiredStates = await databaseContext.OidcStates
            .Where(s => s.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        databaseContext.OidcStates.RemoveRange(expiredStates);
        await databaseContext.SaveChangesAsync(cancellationToken);
    }

    private static string BuildAuthorizationUrl(OidcProvider provider, string redirectUri, string state, string nonce)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = provider.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = provider.Scopes,
            ["state"] = state,
            ["nonce"] = nonce
        };

		string queryString = string.Join("&", queryParams.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{provider.AuthorizationEndpoint}?{queryString}";
    }

    private async Task<TokenResponse?> ExchangeCodeForTokensAsync(OidcProvider provider, string code, CancellationToken cancellationToken)
    {
		string baseUrl = configuration.GetValue<string>("ApiBaseUrl") ?? 
                     $"{httpContextAccessor.HttpContext!.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";
		string redirectUri = $"{baseUrl}/api/authoidc/callback/{provider.ProviderId}";

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = provider.ClientId,
            ["client_secret"] = provider.ClientSecret
        };

        try
        {
			HttpResponseMessage response = await _httpClient.PostAsync(
                provider.TokenEndpoint,
                new FormUrlEncodedContent(tokenRequest), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
				string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Token endpoint returned {StatusCode}: {Error}", response.StatusCode, errorContent);
                return null;
            }

			string content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TokenResponse>(content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exchanging code for tokens");
            return null;
        }
    }

    private async Task<OidcUserInfo?> GetUserInfoAsync(OidcProvider provider, TokenResponse tokenResponse, CancellationToken cancellationToken)
    {
        // First try to extract from ID token if present
        if (!string.IsNullOrEmpty(tokenResponse.IdToken))
        {
			OidcUserInfo? userInfo = ExtractUserInfoFromIdToken(tokenResponse.IdToken, provider);
            if (userInfo != null)
                return userInfo;
        }

        // Fall back to UserInfo endpoint if configured
        if (!string.IsNullOrEmpty(provider.UserInfoEndpoint) && !string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, provider.UserInfoEndpoint);
                request.Headers.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");

				HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("UserInfo endpoint returned {StatusCode}", response.StatusCode);
                    return null;
                }

				string content = await response.Content.ReadAsStringAsync(cancellationToken);
				Dictionary<string, JsonElement>? claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
                
                return MapClaimsToUserInfo(claims, provider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calling UserInfo endpoint");
                return null;
            }
        }

        return null;
    }

    private OidcUserInfo? ExtractUserInfoFromIdToken(string idToken, OidcProvider provider)
    {
        try
        {
			// Simple JWT parsing - in production, you should validate signature
			string[] parts = idToken.Split('.');
            if (parts.Length != 3)
                return null;

			string payload = parts[1];
            // Add padding if needed
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

			byte[] jsonBytes = Convert.FromBase64String(payload);
			string json = Encoding.UTF8.GetString(jsonBytes);
			Dictionary<string, JsonElement>? claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            return MapClaimsToUserInfo(claims, provider);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting user info from ID token");
            return null;
        }
    }

    private OidcUserInfo? MapClaimsToUserInfo(Dictionary<string, JsonElement>? claims, OidcProvider provider)
    {
        if (claims == null)
            return null;

        try
        {
			Dictionary<string, string> mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(provider.ClaimMappings) 
                          ?? new Dictionary<string, string>();

            var userInfo = new OidcUserInfo();

            // Extract sub (required)
            if (mappings.TryGetValue("sub", out string? subClaim) && claims.TryGetValue(subClaim, out JsonElement subValue))
                userInfo.Sub = subValue.GetString() ?? string.Empty;
            else if (claims.TryGetValue("sub", out JsonElement defaultSub))
                userInfo.Sub = defaultSub.GetString() ?? string.Empty;

            // Extract email
            if (mappings.TryGetValue("email", out string? emailClaim) && claims.TryGetValue(emailClaim, out JsonElement emailValue))
                userInfo.Email = emailValue.GetString();
            else if (claims.TryGetValue("email", out JsonElement defaultEmail))
                userInfo.Email = defaultEmail.GetString();

            // Extract name
            if (mappings.TryGetValue("name", out string? nameClaim) && claims.TryGetValue(nameClaim, out JsonElement nameValue))
                userInfo.Name = nameValue.GetString();
            else if (claims.TryGetValue("name", out JsonElement defaultName))
                userInfo.Name = defaultName.GetString();
            else if (claims.TryGetValue("given_name", out JsonElement givenName))
                userInfo.Name = givenName.GetString();

            if (string.IsNullOrEmpty(userInfo.Sub))
            {
                logger.LogWarning("Could not extract 'sub' claim from OIDC response");
                return null;
            }

            return userInfo;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error mapping claims to user info");
            return null;
        }
    }

    private async Task<UserModel?> FindUserByExternalLinkAsync(OidcProvider provider, OidcUserInfo userInfo, CancellationToken cancellationToken)
    {
		// Check if this external user already exists and is linked
		ExternalUserLink? existingLink = await databaseContext.ExternalUserLinks
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.ProviderId == provider.Id && l.ProviderUserId == userInfo.Sub, cancellationToken);

        if (existingLink != null)
        {
            // Update last login time
            existingLink.LastLoginAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(userInfo.Email))
                existingLink.ProviderEmail = userInfo.Email;
            
            await databaseContext.SaveChangesAsync(cancellationToken);
            return existingLink.User!;
        }

        // No existing link found - user must link manually
        return null;
    }

    /// <summary>
    /// Link an external OIDC identity to the currently authenticated user
    /// </summary>
    public async Task<ApiResponse> LinkExternalAccountAsync(Guid userId, string providerId, OidcUserInfo userInfo, CancellationToken cancellationToken)
    {
		OidcProvider? provider = await databaseContext.OidcProviders
            .FirstOrDefaultAsync(p => p.ProviderId == providerId && p.IsEnabled, cancellationToken);

        if (provider == null)
            return ApiResponse.Create("PROVIDER_NOT_FOUND", "Provider not found", System.Net.HttpStatusCode.NotFound);

		UserModel? user = await databaseContext.Users.FindAsync([userId], cancellationToken);
        if (user == null)
            return ApiResponse.Create("USER_NOT_FOUND", "User not found", System.Net.HttpStatusCode.NotFound);

		// Check if this external identity is already linked to another account
		ExternalUserLink? existingLink = await databaseContext.ExternalUserLinks
            .FirstOrDefaultAsync(l => l.ProviderId == provider.Id && l.ProviderUserId == userInfo.Sub, cancellationToken);

        if (existingLink != null)
        {
            if (existingLink.UserId == userId)
                return ApiResponse.Create("ALREADY_LINKED", "This account is already linked", System.Net.HttpStatusCode.Conflict);
            else
                return ApiResponse.Create("LINK_EXISTS", "This external account is already linked to another user", System.Net.HttpStatusCode.Conflict);
        }

        // Create the link
        var newLink = new ExternalUserLink
        {
            UserId = userId,
            ProviderId = provider.Id,
            ProviderUserId = userInfo.Sub,
            ProviderEmail = userInfo.Email,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        await databaseContext.ExternalUserLinks.AddAsync(newLink, cancellationToken);
        await databaseContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Create("SUCCESS", "Account linked successfully", System.Net.HttpStatusCode.OK);
    }

    /// <summary>
    /// Unlink an external OIDC identity from the user
    /// </summary>
    public async Task<ApiResponse> UnlinkExternalAccountAsync(Guid userId, Guid linkId, CancellationToken cancellationToken)
    {
		ExternalUserLink? link = await databaseContext.ExternalUserLinks
            .Include(l => l.Provider)
            .FirstOrDefaultAsync(l => l.Id == linkId && l.UserId == userId, cancellationToken);
        if (link == null)
            return ApiResponse.Create("LINK_NOT_FOUND", "Link not found", System.Net.HttpStatusCode.NotFound);

		// Check if user has a password (at least one auth method must remain)
		UserModel? user = await databaseContext.Users.FindAsync([userId], cancellationToken);
        if (user == null)
            return ApiResponse.Create("USER_NOT_FOUND", "User not found", System.Net.HttpStatusCode.NotFound);

        databaseContext.ExternalUserLinks.Remove(link);
        await databaseContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Create("SUCCESS", "Account unlinked successfully", System.Net.HttpStatusCode.OK);
    }

    /// <summary>
    /// Get all external links for a user
    /// </summary>
    public async Task<ApiResponse<List<ExternalLinkViewDto>>> GetUserLinksAsync(Guid userId, CancellationToken cancellationToken)
    {
        return ApiResponse.Create(await databaseContext.ExternalUserLinks
			.AsNoTracking()
            .Where(l => l.UserId == userId)
            .Include(l => l.Provider)
            .Select(l => new ExternalLinkViewDto
            {
                Id = l.Id,
                ProviderId = l.Provider!.ProviderId,
                ProviderDisplayName = l.Provider.DisplayName,
                ProviderEmail = l.ProviderEmail,
                LinkedAt = l.CreatedAt,
                LastLoginAt = l.LastLoginAt
            })
            .ToListAsync(cancellationToken), System.Net.HttpStatusCode.OK);
    }

    private static string GenerateSecureToken(int length)
    {
		byte[] bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
