using DSaladin.Frnq.Api.Quote;

namespace DSaladin.Frnq.Api.Position;

public class PositionSnapshot
{
    public string UserId { get; set; } = string.Empty;
    public int QuoteId { get; set; } // New FK to QuoteModel
    public DateTime Date { get; set; }
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// The amount of the asset held in the position at the time of the snapshot.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// The total amount invested in the position, which is the sum of the amount multiplied by the price per unit and any fees incurred.
    /// </summary>
    public decimal Invested { get; set; }
    /// <summary>
    /// The market value of the position at the time of the snapshot
    /// </summary>
    public decimal MarketPricePerUnit { get; set; }
    /// <summary>
    /// The total fees associated with the position at the time of the snapshot.
    /// </summary>
    public decimal TotalFees { get; set; }

    /// <summary>
    /// Calculates the total value of the position, which is the market value multiplied by the amount held in the position.
    /// </summary>
    public decimal TotalValue => MarketPricePerUnit * Amount;


    /// <summary>
    /// The unrealized gain for the current position at the snapshot date.
    /// </summary>
    public decimal UnrealizedGain { get; set; }
    /// <summary>
    /// The total realized gain for this symbol up to the snapshot date (all time).
    /// </summary>
    public decimal RealizedGain { get; set; }
    public decimal TotalGain => UnrealizedGain + RealizedGain;
}