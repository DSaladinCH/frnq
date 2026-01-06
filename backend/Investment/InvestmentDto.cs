using System.ComponentModel.DataAnnotations;
using DSaladin.Frnq.Api.Validation;

namespace DSaladin.Frnq.Api.Investment;

/// <summary>
/// Data Transfer Object for creating or updating an investment.
/// </summary>
[InvestmentValidation]
public class InvestmentDto
{
    public int QuoteId { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string QuoteSymbol { get; set; } = string.Empty;
    public required DateTime Date { get; set; }

    public required InvestmentType Type { get; set; } = InvestmentType.Buy;

	[MinValue(0, false)]
    public required decimal Amount { get; set; }

	[MinValue(0, false)]
    public required decimal PricePerUnit { get; set; }

	[MinValue(0)]
    public required decimal TotalFees { get; set; }
}