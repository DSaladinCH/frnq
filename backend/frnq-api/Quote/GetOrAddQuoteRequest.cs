namespace DSaladin.Frnq.Api.Quote;

public class GetOrAddQuoteRequest
{
    public string ProviderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
}