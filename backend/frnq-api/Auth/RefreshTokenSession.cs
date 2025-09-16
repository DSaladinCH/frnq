using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSaladin.Frnq.Api.Auth;

[Table("refresh_token_session")]
public class RefreshTokenSession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public UserModel User { get; set; } = null!;
    
    public string Token { get; set; } = string.Empty;
    
    public DateTime ExpiryTime { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string? DeviceInfo { get; set; }
    
    public string? IpAddress { get; set; }
    
    public bool IsActive { get; set; } = true;
}