using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Auth;

/// <summary>
/// Links external OIDC provider users to local user accounts
/// Allows same user to login via multiple providers
/// </summary>
[Table("external_user_link")]
[Index(nameof(ProviderId), nameof(ProviderUserId), IsUnique = true)]
public class ExternalUserLink
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the local user account
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Reference to the OIDC provider
    /// </summary>
    [Required]
    public Guid ProviderId { get; set; }

    /// <summary>
    /// The user's unique identifier from the external provider (typically "sub" claim)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ProviderUserId { get; set; } = string.Empty;

    /// <summary>
    /// Email from the external provider (for reference/auditing)
    /// </summary>
    [MaxLength(255)]
    public string? ProviderEmail { get; set; }

    /// <summary>
    /// When this link was created (first login via this provider)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this user last logged in via this provider
    /// </summary>
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the local user
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual UserModel? User { get; set; }

    /// <summary>
    /// Navigation property to the provider
    /// </summary>
    [ForeignKey(nameof(ProviderId))]
    public virtual OidcProvider? Provider { get; set; }
}
