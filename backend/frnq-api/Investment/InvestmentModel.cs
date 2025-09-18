using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Quote;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Investment;

[Table("investment")]
public class InvestmentModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int QuoteId { get; set; } // FK to QuoteModel
    public DateTime Date { get; set; }

    public InvestmentType Type { get; set; } = InvestmentType.Buy;

    /// <summary>
    /// The amount of the investment, e.g. number of shares and for dividends, this is the total amount received.
    /// </summary>
    public decimal Amount { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal TotalFees { get; set; }

    public virtual UserModel User { get; set; } = null!;
    public virtual QuoteModel Quote { get; set; } = null!;
}