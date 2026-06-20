using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Group;

namespace DSaladin.Frnq.Api.GeneralFee;

[Table("general_fee")]
public class GeneralFeeModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// The date when the fee was incurred.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The amount of the fee.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Description of the fee (e.g., "Advisory Fee", "Custody Fee", etc.)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional: The group this fee is assigned to. If null, it's a portfolio-level fee.
    /// </summary>
    public int? GroupId { get; set; }

    /// <summary>
    /// Timestamp when the fee was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual UserModel User { get; set; } = null!;

    public virtual QuoteGroup? Group { get; set; }
}
