using DSaladin.Frnq.Api.Group;

namespace DSaladin.Frnq.Api.Quote;

public class QuoteDto
{
	public int Id { get; set; } // New autoincrement PK
	public string ProviderId { get; set; } = string.Empty;
	public string Symbol { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string CustomName { get; set; } = string.Empty;
	public string ExchangeDisposition { get; set; } = string.Empty;
	public string TypeDisposition { get; set; } = string.Empty;
	public string Currency { get; set; } = string.Empty;
	public DateTime LastUpdatedPrices { get; set; }
	public QuoteGroup? Group { get; set; }

	public static QuoteDto FromModel(QuoteModel model)
	{
		return new QuoteDto
		{
			Id = model.Id,
			ProviderId = model.ProviderId,
			Symbol = model.Symbol,
			Name = model.Name,
			CustomName = model.Names.FirstOrDefault()?.CustomName ?? string.Empty,
			ExchangeDisposition = model.ExchangeDisposition,
			TypeDisposition = model.TypeDisposition,
			Currency = model.Currency,
			LastUpdatedPrices = model.LastUpdatedPrices,
			Group = model.Mappings.FirstOrDefault()?.Group
		};
	}
}