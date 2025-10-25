using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSaladin.Frnq.Api.Auth;

/// <summary>
/// Stores OAuth2/OIDC state and CSRF tokens temporarily
/// Used to prevent CSRF attacks during the OAuth flow
/// </summary>
[Table("oidc_state")]
public class OidcState
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>
    /// The state parameter sent to the authorization endpoint
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Reference to which provider this state belongs to
    /// </summary>
    [Required]
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Optional nonce for additional security
    /// </summary>
    [MaxLength(200)]
    public string? Nonce { get; set; }

    /// <summary>
    /// The return URL after successful authentication
    /// </summary>
    [MaxLength(500)]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Optional user ID for account linking flow
    /// </summary>
    public Guid? LinkingUserId { get; set; }

    /// <summary>
    /// When this state was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this state expires (typically 10-15 minutes)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether this state has been used (to prevent replay attacks)
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Navigation property to the provider
    /// </summary>
    [ForeignKey(nameof(ProviderId))]
    public virtual OidcProvider? Provider { get; set; }
}
