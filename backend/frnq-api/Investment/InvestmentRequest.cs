namespace DSaladin.Frnq.Api.Investment;

public class InvestmentRequest
{
    public int QuoteId { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string QuoteSymbol { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    public InvestmentType Type { get; set; } = InvestmentType.Buy;

    public decimal Amount { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal TotalFees { get; set; }
}