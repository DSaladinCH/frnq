using System.ComponentModel.DataAnnotations.Schema;
using DSaladin.Frnq.Api.Auth;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Quote;

[Table("quote_name")]
[Index(nameof(UserId), nameof(QuoteId), IsUnique = true)]
public class QuoteName
{
	public Guid UserId { get; set; }
	public int QuoteId { get; set; }
	public string CustomName { get; set; } = string.Empty;

	public virtual UserModel User { get; set; } = null!;
	public virtual QuoteModel Quote { get; set; } = null!;
}