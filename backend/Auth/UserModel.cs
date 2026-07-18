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

public enum NumberFormat
{
    English = 0,  // en-US: 1,234.56
    German = 1,   // de-DE: 1.234,56
    Swiss = 2     // de-CH: 1'234.56
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

    public NumberFormat NumberFormat { get; set; } = NumberFormat.English;

	[MinLength(2)]
	public int ForecastNumberOfInvestments { get; set; } = 5;

    public virtual ICollection<InvestmentModel> Investments { get; set; } = [];
    public virtual ICollection<ExternalUserLink> ExternalLinks { get; set; } = [];
}