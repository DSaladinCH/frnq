using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DSaladin.Frnq.Api.Quote;

[Table("quote_price")]
public class QuotePrice
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; } // Optional: add PK for quote_price if needed
    public int QuoteId { get; set; } // FK to QuoteModel
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal Close { get; set; }
    public decimal Low { get; set; }
    public decimal High { get; set; }
    public decimal? AdjustedClose { get; set; }
    
    [NotMapped]
    [JsonIgnore]
    public string Currency { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual QuoteModel Quote { get; set; } = null!;
}