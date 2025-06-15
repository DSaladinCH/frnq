using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Quote;

[Table("quote_price")]
[PrimaryKey(nameof(ProviderId), nameof(Symbol), nameof(Date))]
public class QuotePrice
{
    public string ProviderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal Close { get; set; }
    public decimal Low { get; set; }
    public decimal High { get; set; }
    public decimal? AdjustedClose { get; set; }

    public virtual QuoteModel Quote { get; set; } = null!;
}