namespace DSaladin.Frnq.Api.Forecast;

public class ForecastQuoteDto
{
	public int QuoteId { get; set; }
	public double Median { get; set; }
	public double Lower { get; set; }
	public double Upper { get; set; }
}