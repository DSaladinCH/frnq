using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DSaladin.Frnq.Api.Quote;

[Table("quote")]
public class QuoteModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; } // New autoincrement PK
    public string ProviderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExchangeDisposition { get; set; } = string.Empty;
    public string TypeDisposition { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    /// <summary>
    /// The last time the quote prices were updated in the database.
    /// </summary>
    public DateTime LastUpdatedPrices { get; set; }

    [JsonIgnore]
    public virtual ICollection<QuotePrice> Prices { get; set; } = [];
    [JsonIgnore]
    public virtual ICollection<QuoteGroupMapping> Mappings { get; set; } = [];

    public static string GetSenatizedName(string name)
    {
        return name
            .Replace("\"", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }
}