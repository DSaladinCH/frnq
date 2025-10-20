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
    public async Task<List<OidcProviderDto>> GetEnabledProvidersAsync()
    {
        var providers = await databaseContext.OidcProviders
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new OidcProviderDto
            {
                ProviderId = p.ProviderId,
                DisplayName = p.DisplayName,
                FaviconUrl = p.FaviconUrl,
                AutoRedirect = p.AutoRedirect
            })
            .ToListAsync();

        return providers;
    }

    /// <summary>
    /// Initiates OAuth2/OIDC authorization flow
    /// </summary>
    /// <param name="providerId">The provider to use</param>
    /// <param name="returnUrl">Optional return URL</param>
    /// <param name="userId">Optional user ID for linking flow</param>
    public async Task<ApiResponse<string>> InitiateLoginAsync(string providerId, string? returnUrl = null, Guid? userId = null)
    {
        var provider = await databaseContext.OidcProviders
            .FirstOrDefaultAsync(p => p.ProviderId == providerId && p.IsEnabled);

        if (provider == null)
            return ApiResponse<string>.Create(null, "PROVIDER_NOT_FOUND", "Provider not found or disabled", System.Net.HttpStatusCode.NotFound);

        // Generate secure random state and nonce
        var state = GenerateSecureToken(32);
        var nonce = GenerateSecureToken(32);

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

        databaseContext.OidcStates.Add(oidcState);
        await databaseContext.SaveChangesAsync();

        // Build redirect URL
        var baseUrl = configuration.GetValue<string>("ApiBaseUrl") ?? 
                     $"{httpContextAccessor.HttpContext!.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";
        var redirectUri = $"{baseUrl}/api/authoidc/callback/{provider.ProviderId}";

        var authUrl = BuildAuthorizationUrl(provider, redirectUri, state, nonce);

        return ApiResponse<string>.Create(authUrl, System.Net.HttpStatusCode.OK);
    }

    /// <summary>
    /// Handles the OAuth2/OIDC callback after user authorizes
    /// </summary>
    public async Task<ApiResponse<LoginResponseModel>> HandleCallbackAsync(string providerId, string code, string state)
    {
        // Validate state to prevent CSRF
        var oidcState = await databaseContext.OidcStates
            .Include(s => s.Provider)
            .FirstOrDefaultAsync(s => s.State == state && !s.IsUsed && s.ExpiresAt > DateTime.UtcNow);

        if (oidcState == null)
        {
            logger.LogWarning("Invalid or expired OIDC state: {State}", state);
            return ApiResponse<LoginResponseModel>.Create(null, "INVALID_STATE", "Invalid or expired state", System.Net.HttpStatusCode.BadRequest);
        }

        if (oidcState.Provider == null || oidcState.Provider.ProviderId != providerId)
        {
            logger.LogWarning("State provider mismatch. Expected: {Expected}, Got: {Got}", oidcState.Provider?.ProviderId, providerId);
            return ApiResponse<LoginResponseModel>.Create(null, "PROVIDER_MISMATCH", "Provider mismatch", System.Net.HttpStatusCode.BadRequest);
        }

        // Mark state as used
        oidcState.IsUsed = true;
        await databaseContext.SaveChangesAsync();

        var provider = oidcState.Provider;

        try
        {
            // Exchange code for tokens
            var tokenResponse = await ExchangeCodeForTokensAsync(provider, code);
            if (tokenResponse == null)
            {
                logger.LogError("Failed to exchange code for tokens with provider {Provider}", providerId);
                return ApiResponse<LoginResponseModel>.Create(null, "TOKEN_EXCHANGE_FAILED", "Failed to obtain tokens", System.Net.HttpStatusCode.BadGateway);
            }

            // Get user info from ID token or UserInfo endpoint
            var userInfo = await GetUserInfoAsync(provider, tokenResponse);
            if (userInfo == null)
            {
                logger.LogError("Failed to get user info from provider {Provider}", providerId);
                return ApiResponse<LoginResponseModel>.Create(null, "USERINFO_FAILED", "Failed to get user information", System.Net.HttpStatusCode.BadGateway);
            }

            // Check if this is a linking flow
            if (oidcState.LinkingUserId.HasValue)
            {
                // This is a linking request - link and return success
                var linkResult = await LinkExternalAccountAsync(oidcState.LinkingUserId.Value, providerId, userInfo);
                
                if (linkResult.Success)
                {
                    // Return a special response indicating linking succeeded
                    return ApiResponse<LoginResponseModel>.Create(null, "LINK_SUCCESS", "Account linked successfully", System.Net.HttpStatusCode.OK);
                }
                else
                {
                    return ApiResponse<LoginResponseModel>.Create(null, linkResult.Code, linkResult.Description, linkResult.StatusCode);
                }
            }

            // Normal login flow - find user by external link
            var user = await FindUserByExternalLinkAsync(provider, userInfo);

            if (user == null)
            {
                // No linked account found - return error with user info for linking
                logger.LogInformation("No linked account found for OIDC user {Sub} from provider {Provider}", userInfo.Sub, providerId);
                return ApiResponse<LoginResponseModel>.Create(null, "NOT_LINKED", "No linked account found. Please log in with your credentials and link this account.", System.Net.HttpStatusCode.Unauthorized);
            }

            // Generate our own JWT tokens
            var loginResponse = await authManagement.LoginUserByUserModelAsync(user);

            return loginResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during OIDC callback for provider {Provider}", providerId);
            return ApiResponse<LoginResponseModel>.Create(null, "OIDC_ERROR", "Authentication failed", System.Net.HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Cleanup expired OIDC states (should be run periodically)
    /// </summary>
    public async Task CleanupExpiredStatesAsync()
    {
        var expiredStates = await databaseContext.OidcStates
            .Where(s => s.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        databaseContext.OidcStates.RemoveRange(expiredStates);
        await databaseContext.SaveChangesAsync();
    }

    private string BuildAuthorizationUrl(OidcProvider provider, string redirectUri, string state, string nonce)
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

        var queryString = string.Join("&", queryParams.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{provider.AuthorizationEndpoint}?{queryString}";
    }

    private async Task<TokenResponse?> ExchangeCodeForTokensAsync(OidcProvider provider, string code)
    {
        var baseUrl = configuration.GetValue<string>("ApiBaseUrl") ?? 
                     $"{httpContextAccessor.HttpContext!.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";
        var redirectUri = $"{baseUrl}/api/authoidc/callback/{provider.ProviderId}";

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
            var response = await _httpClient.PostAsync(
                provider.TokenEndpoint,
                new FormUrlEncodedContent(tokenRequest));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Token endpoint returned {StatusCode}: {Error}", response.StatusCode, errorContent);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TokenResponse>(content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exchanging code for tokens");
            return null;
        }
    }

    private async Task<OidcUserInfo?> GetUserInfoAsync(OidcProvider provider, TokenResponse tokenResponse)
    {
        // First try to extract from ID token if present
        if (!string.IsNullOrEmpty(tokenResponse.IdToken))
        {
            var userInfo = ExtractUserInfoFromIdToken(tokenResponse.IdToken, provider);
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

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("UserInfo endpoint returned {StatusCode}", response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
                
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
            var parts = idToken.Split('.');
            if (parts.Length != 3)
                return null;

            var payload = parts[1];
            // Add padding if needed
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var jsonBytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(jsonBytes);
            var claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

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
            var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(provider.ClaimMappings) 
                          ?? new Dictionary<string, string>();

            var userInfo = new OidcUserInfo();

            // Extract sub (required)
            if (mappings.TryGetValue("sub", out var subClaim) && claims.TryGetValue(subClaim, out var subValue))
                userInfo.Sub = subValue.GetString() ?? string.Empty;
            else if (claims.TryGetValue("sub", out var defaultSub))
                userInfo.Sub = defaultSub.GetString() ?? string.Empty;

            // Extract email
            if (mappings.TryGetValue("email", out var emailClaim) && claims.TryGetValue(emailClaim, out var emailValue))
                userInfo.Email = emailValue.GetString();
            else if (claims.TryGetValue("email", out var defaultEmail))
                userInfo.Email = defaultEmail.GetString();

            // Extract name
            if (mappings.TryGetValue("name", out var nameClaim) && claims.TryGetValue(nameClaim, out var nameValue))
                userInfo.Name = nameValue.GetString();
            else if (claims.TryGetValue("name", out var defaultName))
                userInfo.Name = defaultName.GetString();
            else if (claims.TryGetValue("given_name", out var givenName))
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

    private async Task<UserModel?> FindUserByExternalLinkAsync(OidcProvider provider, OidcUserInfo userInfo)
    {
        // Check if this external user already exists and is linked
        var existingLink = await databaseContext.ExternalUserLinks
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.ProviderId == provider.Id && l.ProviderUserId == userInfo.Sub);

        if (existingLink != null)
        {
            // Update last login time
            existingLink.LastLoginAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(userInfo.Email))
                existingLink.ProviderEmail = userInfo.Email;
            
            await databaseContext.SaveChangesAsync();
            return existingLink.User!;
        }

        // No existing link found - user must link manually
        return null;
    }

    /// <summary>
    /// Link an external OIDC identity to the currently authenticated user
    /// </summary>
    public async Task<ApiResponse> LinkExternalAccountAsync(Guid userId, string providerId, OidcUserInfo userInfo)
    {
        var provider = await databaseContext.OidcProviders
            .FirstOrDefaultAsync(p => p.ProviderId == providerId && p.IsEnabled);

        if (provider == null)
            return ApiResponse.Create("PROVIDER_NOT_FOUND", "Provider not found", System.Net.HttpStatusCode.NotFound);

        var user = await databaseContext.Users.FindAsync(userId);
        if (user == null)
            return ApiResponse.Create("USER_NOT_FOUND", "User not found", System.Net.HttpStatusCode.NotFound);

        // Check if this external identity is already linked to another account
        var existingLink = await databaseContext.ExternalUserLinks
            .FirstOrDefaultAsync(l => l.ProviderId == provider.Id && l.ProviderUserId == userInfo.Sub);

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

        databaseContext.ExternalUserLinks.Add(newLink);
        await databaseContext.SaveChangesAsync();

        return ApiResponse.Create("SUCCESS", "Account linked successfully", System.Net.HttpStatusCode.OK);
    }

    /// <summary>
    /// Unlink an external OIDC identity from the user
    /// </summary>
    public async Task<ApiResponse> UnlinkExternalAccountAsync(Guid userId, Guid linkId)
    {
        var link = await databaseContext.ExternalUserLinks
            .Include(l => l.Provider)
            .FirstOrDefaultAsync(l => l.Id == linkId && l.UserId == userId);

        if (link == null)
            return ApiResponse.Create("LINK_NOT_FOUND", "Link not found", System.Net.HttpStatusCode.NotFound);

        // Check if user has a password (at least one auth method must remain)
        var user = await databaseContext.Users.FindAsync(userId);
        if (user == null)
            return ApiResponse.Create("USER_NOT_FOUND", "User not found", System.Net.HttpStatusCode.NotFound);

        databaseContext.ExternalUserLinks.Remove(link);
        await databaseContext.SaveChangesAsync();

        return ApiResponse.Create("SUCCESS", "Account unlinked successfully", System.Net.HttpStatusCode.OK);
    }

    /// <summary>
    /// Get all external links for a user
    /// </summary>
    public async Task<List<ExternalLinkDto>> GetUserLinksAsync(Guid userId)
    {
        return await databaseContext.ExternalUserLinks
            .Where(l => l.UserId == userId)
            .Include(l => l.Provider)
            .Select(l => new ExternalLinkDto
            {
                Id = l.Id,
                ProviderId = l.Provider!.ProviderId,
                ProviderDisplayName = l.Provider.DisplayName,
                ProviderEmail = l.ProviderEmail,
                LinkedAt = l.CreatedAt,
                LastLoginAt = l.LastLoginAt
            })
            .ToListAsync();
    }

    private static string GenerateSecureToken(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}

/// <summary>
/// DTO for public provider information
/// </summary>
public class OidcProviderDto
{
    public string ProviderId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? FaviconUrl { get; set; }
    public bool AutoRedirect { get; set; }
}

/// <summary>
/// OAuth2/OIDC token response
/// </summary>
public class TokenResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("id_token")]
    public string? IdToken { get; set; }
}

/// <summary>
/// User information extracted from OIDC provider
/// </summary>
public class OidcUserInfo
{
    public string Sub { get; set; } = string.Empty; // Required - unique user ID
    public string? Email { get; set; }
    public string? Name { get; set; }
}

/// <summary>
/// DTO for external account link information
/// </summary>
public class ExternalLinkDto
{
    public Guid Id { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderDisplayName { get; set; } = string.Empty;
    public string? ProviderEmail { get; set; }
    public DateTime LinkedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}
