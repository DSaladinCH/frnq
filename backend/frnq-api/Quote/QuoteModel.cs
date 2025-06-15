using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Quote;

[Table("quote")]
[PrimaryKey(nameof(ProviderId), nameof(Symbol))]
public class QuoteModel
{
    public string ProviderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExchangeDisposition { get; set; } = string.Empty;
    public string TypeDisposition { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }

    public virtual ICollection<QuotePrice> Prices { get; set; } = [];
}