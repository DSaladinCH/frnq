namespace DSaladin.Frnq.Api.GeneralFee;

/// <summary>
/// Summary of general fees for a group or portfolio.
/// </summary>
public class GroupFeesSummaryDto
{
    /// <summary>
    /// The group ID. Null if this is a portfolio-level fee summary.
    /// </summary>
    public int? GroupId { get; set; }

    /// <summary>
    /// The name of the group. Empty/null if this is a portfolio-level fee summary.
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Total general fees for this group/portfolio in the requested date range.
    /// </summary>
    public decimal TotalGeneralFees { get; set; }

    /// <summary>
    /// Details of individual fees (optional, for granular reporting).
    /// </summary>
    public List<GeneralFeeViewDto> FeeDetails { get; set; } = [];
}
