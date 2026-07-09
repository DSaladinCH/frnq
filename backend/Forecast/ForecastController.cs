using DSaladin.Frnq.Api.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Forecast;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ForecastController(ForecastManagement forecastManagement) : ControllerBase
{
	[HttpGet]
	[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
	public async Task<ApiResponse> GetForecast(
		[FromQuery] bool includeContributions = false,
		CancellationToken cancellationToken = default)
	{
		return await forecastManagement.CreateForecast(includeContributions, cancellationToken);
	}
}