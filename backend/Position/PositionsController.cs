using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.ModelBinders;
using DSaladin.Frnq.Api.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Position;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PositionsController(PositionManagement positionManagement) : ControllerBase
{
	[HttpGet]
	public async Task<ApiResponse> GetPositions(
		[ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime? from,
		[ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime? to,
		CancellationToken cancellationToken)
	{
		return await positionManagement.GetPositionsAsync(from, to, cancellationToken);
	}
}