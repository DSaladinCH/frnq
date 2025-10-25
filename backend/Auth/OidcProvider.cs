using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSaladin.Frnq.Api.Auth;

/// <summary>
/// Represents an OIDC provider configuration stored in the database
/// </summary>
[Table("oidc_provider")]
public class OidcProvider
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>
    /// Unique identifier for the provider (e.g., "google", "azure", "keycloak")
    /// Used in URLs and as a reference
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the provider (e.g., "Google", "Azure AD")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2/OIDC Authorization endpoint
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2/OIDC Token endpoint
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// OIDC UserInfo endpoint
    /// </summary>
    [MaxLength(500)]
    public string? UserInfoEndpoint { get; set; }

    /// <summary>
    /// OAuth2/OIDC Client ID
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2/OIDC Client Secret (encrypted in a real production system)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Space-separated scopes to request (e.g., "openid profile email")
    /// </summary>
    [MaxLength(500)]
    public string Scopes { get; set; } = "openid profile email";

    /// <summary>
    /// Whether this provider is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Display order for the provider buttons
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Base64-encoded favicon image (max 100x100px)
    /// </summary>
    [MaxLength(50000)]
    public string? FaviconUrl { get; set; }

    /// <summary>
    /// Whether to auto-redirect to this provider when only one is enabled
    /// </summary>
    public bool AutoRedirect { get; set; } = false;

    /// <summary>
    /// JSON field mapping configuration for extracting user info from ID token or UserInfo endpoint
    /// Format: {"email": "email", "name": "name", "sub": "sub"}
    /// </summary>
    [MaxLength(2000)]
    public string ClaimMappings { get; set; } = "{\"email\":\"email\",\"name\":\"name\",\"sub\":\"sub\"}";

    /// <summary>
    /// When this provider was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this provider configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
