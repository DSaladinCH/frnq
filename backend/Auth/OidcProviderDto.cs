namespace DSaladin.Frnq.Api.Auth;

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