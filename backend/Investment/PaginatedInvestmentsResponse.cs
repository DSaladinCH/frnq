namespace DSaladin.Frnq.Api.Investment;

public class PaginatedInvestmentsResponse
{
    public required List<InvestmentViewDto> Items { get; set; } = [];
    public required int TotalCount { get; set; }
    public required int Skip { get; set; }
    public required int Take { get; set; }
}
