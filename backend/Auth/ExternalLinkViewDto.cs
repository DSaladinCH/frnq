namespace DSaladin.Frnq.Api.Auth;

/// <summary>
/// DTO for external account link information
/// </summary>
public class ExternalLinkViewDto
{
    public Guid Id { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderDisplayName { get; set; } = string.Empty;
    public string? ProviderEmail { get; set; }
    public DateTime LinkedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}
