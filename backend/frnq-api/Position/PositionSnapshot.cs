using DSaladin.Frnq.Api.Quote;

namespace DSaladin.Frnq.Api.Position;

public class PositionSnapshot
{
    public string UserId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string QuoteSymbol { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// The amount of the asset held in the position.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// The price per unit of the asset at the time of the snapshot.
    /// </summary>
    public decimal PricePerUnit { get; set; }
    /// <summary>
    /// The market value of the position at the time of the snapshot
    /// </summary>
    public decimal MarketPricePerUnit { get; set; }
    /// <summary>
    /// The total fees associated with the position at the time of the snapshot.
    /// </summary>
    public decimal TotalFees { get; set; }

    /// <summary>
    /// Calculates the total amount invested in the position, which is the sum of the amount multiplied by the price per unit and any fees incurred.
    /// </summary>
    public decimal Invested => Amount * PricePerUnit + TotalFees;
    /// <summary>
    /// Calculates the total value of the position, which is the market value multiplied by the amount held in the position.
    /// </summary>
    public decimal TotalValue => MarketPricePerUnit * Amount;
    /// <summary>
    /// Calculates the gain or loss of the position, which is the difference between the market value and the invested amount.
    /// </summary>
    public decimal GainLoss => TotalValue - Invested;

    public virtual QuoteModel Quote { get; set; } = null!;
}