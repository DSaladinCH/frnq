namespace DSaladin.Frnq.Api.Quote.Providers;

public class ProviderRegistry
{
	private readonly Dictionary<string, IFinanceProvider> providers;

	public ProviderRegistry(IEnumerable<IFinanceProvider> providers)
	{
		this.providers = providers.ToDictionary(p => p.InternalId.ToLowerInvariant());
	}

	public IFinanceProvider? GetProvider(string internalId)
	{
		return providers.TryGetValue(internalId.ToLowerInvariant(), out IFinanceProvider? provider) ? provider : null;
	}
}