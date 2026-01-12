using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace DSaladin.Frnq.Api.Auth;

/// <summary>
/// Background service that initializes OIDC providers from configuration
/// and performs periodic cleanup of expired state tokens
/// </summary>
public class OidcInitializationService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OidcInitializationService> _logger;
    private Timer? _cleanupTimer;

    public OidcInitializationService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<OidcInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting OIDC initialization service");

        // Initialize providers from configuration
        await InitializeProvidersAsync(cancellationToken);

        // Start cleanup timer (run every hour)
        _cleanupTimer = new Timer(
            async _ => await CleanupExpiredStatesAsync(cancellationToken),
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromHours(1));

        _logger.LogInformation("OIDC initialization service started");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping OIDC initialization service");
        _cleanupTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async Task InitializeProvidersAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
			DatabaseContext dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

			// Get OIDC providers from configuration
			IConfigurationSection providersConfig = _configuration.GetSection("OidcProviders");
            
            foreach (IConfigurationSection providerSection in providersConfig.GetChildren())
            {
				string providerId = providerSection.Key;
				bool enabled = providerSection.GetValue<bool>("Enabled", false);

                if (!enabled)
                {
                    _logger.LogInformation("OIDC provider {ProviderId} is disabled in configuration", providerId);
                    continue;
                }

				string displayName = providerSection.GetValue<string>("DisplayName") ?? providerId;
				string? authEndpoint = providerSection.GetValue<string>("AuthorizationEndpoint");
				string? tokenEndpoint = providerSection.GetValue<string>("TokenEndpoint");
				string? userInfoEndpoint = providerSection.GetValue<string>("UserInfoEndpoint");
				string? clientId = providerSection.GetValue<string>("ClientId");
				string? clientSecret = providerSection.GetValue<string>("ClientSecret");
				string scopes = providerSection.GetValue<string>("Scopes") ?? "openid profile email";
				bool autoRedirect = providerSection.GetValue<bool>("AutoRedirect", false);
				int displayOrder = providerSection.GetValue<int>("DisplayOrder", 0);
				string claimMappings = providerSection.GetValue<string>("ClaimMappings") ?? "{\"email\":\"email\",\"name\":\"name\",\"sub\":\"sub\"}";
				string? issuerUrl = providerSection.GetValue<string>("IssuerUrl");

                // Validate required fields
                if (string.IsNullOrEmpty(authEndpoint) || 
                    string.IsNullOrEmpty(tokenEndpoint) ||
                    string.IsNullOrEmpty(clientId) ||
                    string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogWarning(
                        "OIDC provider {ProviderId} is missing required configuration. Skipping.",
                        providerId);
                    continue;
                }

                // Fetch favicon if issuer URL is provided
                string? faviconData = null;
                if (!string.IsNullOrEmpty(issuerUrl))
                {
                    faviconData = await FetchAndResizeFaviconAsync(issuerUrl, cancellationToken);
                }

				// Check if provider already exists
				OidcProvider? existingProvider = await dbContext.OidcProviders
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId, cancellationToken);

                if (existingProvider != null)
                {
                    // Update existing provider
                    existingProvider.DisplayName = displayName;
                    existingProvider.AuthorizationEndpoint = authEndpoint;
                    existingProvider.TokenEndpoint = tokenEndpoint;
                    existingProvider.UserInfoEndpoint = userInfoEndpoint;
                    existingProvider.ClientId = clientId;
                    existingProvider.ClientSecret = clientSecret;
                    existingProvider.Scopes = scopes;
                    existingProvider.IsEnabled = enabled;
                    existingProvider.AutoRedirect = autoRedirect;
                    existingProvider.DisplayOrder = displayOrder;
                    existingProvider.ClaimMappings = claimMappings;
                    existingProvider.UpdatedAt = DateTime.UtcNow;
                    
                    // Only update favicon if we successfully fetched a new one
                    if (faviconData != null)
                    {
                        existingProvider.FaviconUrl = faviconData;
                    }

                    _logger.LogInformation("Updated OIDC provider: {ProviderId}", providerId);
                }
                else
                {
					// Create new provider
					OidcProvider newProvider = new OidcProvider
                    {
                        ProviderId = providerId,
                        DisplayName = displayName,
                        AuthorizationEndpoint = authEndpoint,
                        TokenEndpoint = tokenEndpoint,
                        UserInfoEndpoint = userInfoEndpoint,
                        ClientId = clientId,
                        ClientSecret = clientSecret,
                        Scopes = scopes,
                        IsEnabled = enabled,
                        FaviconUrl = faviconData,
                        AutoRedirect = autoRedirect,
                        DisplayOrder = displayOrder,
                        ClaimMappings = claimMappings
                    };

                    await dbContext.OidcProviders.AddAsync(newProvider, cancellationToken);
                    _logger.LogInformation("Created OIDC provider: {ProviderId}", providerId);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("OIDC providers initialized from configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing OIDC providers");
        }
    }

    private async Task<string?> FetchAndResizeFaviconAsync(string issuerUrl, CancellationToken cancellationToken)
    {
        try
        {
			// Parse the issuer URL to get the base domain
			Uri uri = new Uri(issuerUrl);
			string baseUrl = $"{uri.Scheme}://{uri.Host}";
            
            using HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            byte[]? imageBytes = null;
            string? successUrl = null;

			// Try multiple common favicon locations in order of preference
			string[] faviconUrls = new[]
            {
                $"{baseUrl}/favicon.ico",      // Classic favicon
                $"{baseUrl}/favicon.png",      // PNG variant
                $"{baseUrl}/favicon.jpg",      // JPEG variant (rare but possible)
                $"{baseUrl}/icon.png",         // Alternative naming
                $"{baseUrl}/logo.png",         // Some sites use logo.png
            };
            
            foreach (string? faviconUrl in faviconUrls)
            {
                try
                {
					HttpResponseMessage response = await httpClient.GetAsync(faviconUrl, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
						byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                        // Verify it's a valid image by trying to load it
                        if (bytes.Length > 0)
                        {
                            try
                            {
                                using Image testImage = Image.Load(bytes);
                                imageBytes = bytes;
                                successUrl = faviconUrl;
                                _logger.LogDebug("Successfully fetched favicon from {FaviconUrl}", faviconUrl);
                                break;
                            }
                            catch
                            {
                                // Not a valid image, try next URL
                                _logger.LogDebug("Invalid image format at {FaviconUrl}", faviconUrl);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to fetch favicon from {FaviconUrl}", faviconUrl);
                }
            }
            
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _logger.LogWarning("Could not fetch favicon for {IssuerUrl} from any standard location", issuerUrl);
                return null;
            }
            
            // Load and resize the image
            using Image image = Image.Load(imageBytes);

			// Resize to max 100x100 while maintaining aspect ratio
			int targetSize = 100;
            if (image.Width > targetSize || image.Height > targetSize)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(targetSize, targetSize),
                    Mode = ResizeMode.Max
                }));
            }
            
            // Convert to PNG and base64
            using MemoryStream ms = new MemoryStream();
            await image.SaveAsPngAsync(ms, cancellationToken);
			string base64 = Convert.ToBase64String(ms.ToArray());
            
            _logger.LogInformation("Successfully fetched and resized favicon for {IssuerUrl} from {SuccessUrl}", issuerUrl, successUrl);
            return $"data:image/png;base64,{base64}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching favicon for {IssuerUrl}", issuerUrl);
            return null;
        }
    }

    private async Task CleanupExpiredStatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
			OidcManagement oidcManagement = scope.ServiceProvider.GetRequiredService<OidcManagement>();
            
            await oidcManagement.CleanupExpiredStatesAsync(cancellationToken);
            _logger.LogDebug("Cleaned up expired OIDC states");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired OIDC states");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
		GC.SuppressFinalize(this);
    }
}
