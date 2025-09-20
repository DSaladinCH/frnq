using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Quote;

namespace DSaladin.Frnq.Api.Group;

[Table("quote_group")]
public class QuoteGroup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    [JsonIgnore]
    public virtual ICollection<QuoteGroupMapping> Mappings { get; set; } = [];
    
    [JsonIgnore]
    public virtual UserModel User { get; set; } = null!;
}