using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.ModelBinders;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Position;

[ApiController]
[Route("api/[controller]")]
public class PositionsController(PositionManagement positionManagement) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPositions([ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime from, [ModelBinder(BinderType = typeof(FlexibleDateTimeBinder))] DateTime to)
    {
        return Ok(await positionManagement.GetPositionsAsync(from, to));
    }
}