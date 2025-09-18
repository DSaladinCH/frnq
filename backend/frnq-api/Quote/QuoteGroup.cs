using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DSaladin.Frnq.Api.Quote;

[Table("quote_group")]
public class QuoteGroup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual ICollection<QuoteGroupMapping> Mappings { get; set; } = [];
}