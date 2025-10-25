using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DSaladin.Frnq.Api.Investment;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Auth;

public enum DateFormat
{
    English = 0,  // en-US: MM/DD/YYYY
    German = 1    // de-DE: DD.MM.YYYY
}

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

    public DateFormat DateFormat { get; set; } = DateFormat.English;

    public virtual ICollection<InvestmentModel> Investments { get; set; } = [];
    public virtual ICollection<ExternalUserLink> ExternalLinks { get; set; } = [];
}