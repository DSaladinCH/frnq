namespace DSaladin.Frnq.Api.Investment;

public class PaginatedInvestmentsResponse
{
    public List<InvestmentModel> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}
