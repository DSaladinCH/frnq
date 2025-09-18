using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DSaladin.Frnq.Api.Auth;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Quote;

[Table("quote_group_mapping")]
[Index(nameof(UserId), nameof(QuoteId), IsUnique = true)]
public class QuoteGroupMapping
{
    public Guid UserId { get; set; }
    public int QuoteId { get; set; }
    public int GroupId { get; set; }

    public virtual UserModel User { get; set; } = null!;
    public virtual QuoteModel Quote { get; set; } = null!;
    public virtual QuoteGroup Group { get; set; } = null!;
}