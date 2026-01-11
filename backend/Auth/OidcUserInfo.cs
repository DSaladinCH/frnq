namespace DSaladin.Frnq.Api.Auth;

/// <summary>
/// User information extracted from OIDC provider
/// </summary>
public class OidcUserInfo
{
    public string Sub { get; set; } = string.Empty; // Required - unique user ID
    public string? Email { get; set; }
    public string? Name { get; set; }
}