using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSaladin.Frnq.Api.Quote;

[Table("quote_group")]
public class QuoteGroup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public virtual List<QuoteModel> Quotes { get; set; } = [];
}