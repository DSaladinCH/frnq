namespace DSaladin.Frnq.Api.Forecast;

public class ForecastDayDto
{
	public DateOnly Date { get; set; }
	public List<ForecastQuoteDto> Quotes { get; set; } = new();
}