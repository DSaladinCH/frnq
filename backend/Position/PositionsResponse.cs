using System.Collections.Generic;
using DSaladin.Frnq.Api.Position;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.GeneralFee;

namespace DSaladin.Frnq.Api.Position;

public class PositionsResponse
{
	public List<PositionSnapshot> Snapshots { get; set; } = [];
	public List<QuoteViewDto> Quotes { get; set; } = [];
	
	/// <summary>
	/// General fees aggregated by group and portfolio-level for the requested date range.
	/// </summary>
	public List<GroupFeesSummaryDto> GroupFeesSummaries { get; set; } = [];
	
	/// <summary>
	/// Total portfolio-level fees (not assigned to any group) for the requested date range.
	/// </summary>
	public decimal OverallFees { get; set; }
}
