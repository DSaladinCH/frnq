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
	/// Calculates the current value of the position, which is the market value multiplied by the amount held in the position.
	/// </summary>
	public decimal CurrentValue => MarketPricePerUnit * Amount;

	/// <summary>
	/// The total realized gain (profit) for this symbol up to the snapshot date.
	/// This includes: (sale proceeds - cost basis) from all sells + all dividends received.
	/// </summary>
	public decimal RealizedGain { get; set; }

	/// <summary>
	/// Unrealized gain/loss on current holdings = CurrentValue - Invested
	/// </summary>
	public decimal UnrealizedGain => CurrentValue - Invested;

	/// <summary>
	/// Total profit = UnrealizedGain + RealizedGain
	/// </summary>
	public decimal TotalProfit => UnrealizedGain + RealizedGain;

	/// <summary>
	/// Total cash that has been invested over time (including what was sold and reinvested)
	/// </summary>
	public decimal TotalInvestedCash { get; set; }
}