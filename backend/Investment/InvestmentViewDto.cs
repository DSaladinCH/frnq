using System.Text.Json.Serialization;

namespace DSaladin.Frnq.Api.Investment;

/// <summary>
/// Data Transfer Object for viewing an investment.
/// </summary>
public class InvestmentViewDto
{
	public int Id { get; set; }
	public Guid UserId { get; set; }
	public int QuoteId { get; set; }
	public DateTime Date { get; set; }

	public InvestmentType Type { get; set; } = InvestmentType.Buy;

	/// <summary>
	/// The amount of the investment, e.g. number of shares and for dividends, this is the total amount received.
	/// </summary>
	public decimal Amount { get; set; }
	public decimal PricePerUnit { get; set; }
	public decimal TotalFees { get; set; }

	[JsonConstructor]
	private InvestmentViewDto() { }

	public InvestmentViewDto(InvestmentModel investment)
	{
		Id = investment.Id;
		UserId = investment.UserId;
		QuoteId = investment.QuoteId;
		Date = investment.Date;
		Type = investment.Type;
		Amount = investment.Amount;
		PricePerUnit = investment.PricePerUnit;
		TotalFees = investment.TotalFees;
	}
}