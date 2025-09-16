using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Auth;

[Table("user")]
[Index(nameof(Email), IsUnique = true)]
public class UserModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    private string email = string.Empty;
    public string Email
    {
        get => email;
        set => email = value.ToLowerInvariant();
    }
    public string PasswordHash { get; set; } = string.Empty;

    public string Firstname { get; set; } = string.Empty;
}