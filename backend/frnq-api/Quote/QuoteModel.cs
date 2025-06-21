using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
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
    public int GroupId { get; set; } = 0;
    /// <summary>
    /// The last time the quote prices were updated in the database.
    /// </summary>
    public DateTime LastUpdatedPrices { get; set; }

    [JsonIgnore]
    public virtual ICollection<QuotePrice> Prices { get; set; } = [];
    public virtual QuoteGroup? Group { get; set; }
}