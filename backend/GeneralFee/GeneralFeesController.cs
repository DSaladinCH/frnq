using DSaladin.Frnq.Api.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DSaladin.Frnq.Api.GeneralFee;

[ApiController]
[Route("api/general-fees")]
[Authorize]
public class GeneralFeesController(GeneralFeeManagement generalFeeManagement) : ControllerBase
{
	/// <summary>
	/// Creates a new general fee.
	/// </summary>
	[HttpPost]
	public async Task<ActionResult<GeneralFeeViewDto>> CreateGeneralFee(
		[FromBody] GeneralFeeDto request,
		CancellationToken cancellationToken)
	{
		try
		{
			var fee = await generalFeeManagement.CreateGeneralFeeAsync(
				request.Amount,
				request.Date,
				request.Description,
				request.GroupId,
				cancellationToken);

			return CreatedAtAction(nameof(GetGeneralFees), new { id = fee.Id }, fee);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(ApiResponse.Create("INVALID_REQUEST", ex.Message, HttpStatusCode.BadRequest));
		}
	}

	/// <summary>
	/// Gets general fees for the current user with pagination, optionally filtered by group.
	/// </summary>
	[HttpGet]
	public async Task<ActionResult<PaginatedGeneralFeesResponse>> GetGeneralFees(
		[FromQuery] int? groupId = null,
		[FromQuery] int skip = 0,
		[FromQuery] int take = 25,
		CancellationToken cancellationToken = default)
	{
		try
		{
			if (groupId.HasValue)
			{
				var fees = await generalFeeManagement.GetGeneralFeesForGroupAsync(groupId.Value, cancellationToken);
				return Ok(new PaginatedGeneralFeesResponse { Items = fees, TotalCount = fees.Count });
			}
			else
			{
				var (items, totalCount) = await generalFeeManagement.GetGeneralFeesForUserAsync(skip, take, cancellationToken);
				return Ok(new PaginatedGeneralFeesResponse { Items = items, TotalCount = totalCount });
			}
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(ApiResponse.Create("INVALID_REQUEST", ex.Message, HttpStatusCode.BadRequest));
		}
	}

	/// <summary>
	/// Updates an existing general fee.
	/// </summary>
	[HttpPut("{id}")]
	public async Task<ActionResult<GeneralFeeViewDto>> UpdateGeneralFee(
		[FromRoute] int id,
		[FromBody] GeneralFeeDto request,
		CancellationToken cancellationToken)
	{
		try
		{
			var fee = await generalFeeManagement.UpdateGeneralFeeAsync(
				id,
				request.Amount,
				request.Date,
				request.Description,
				request.GroupId,
				cancellationToken);

			return Ok(fee);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(ApiResponse.Create("INVALID_REQUEST", ex.Message, HttpStatusCode.BadRequest));
		}
	}

	/// <summary>
	/// Deletes a general fee.
	/// </summary>
	[HttpDelete("{id}")]
	public async Task<IActionResult> DeleteGeneralFee(
		[FromRoute] int id,
		CancellationToken cancellationToken)
	{
		try
		{
			await generalFeeManagement.DeleteGeneralFeeAsync(id, cancellationToken);
			return NoContent();
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(ApiResponse.Create("INVALID_REQUEST", ex.Message, HttpStatusCode.BadRequest));
		}
	}
}

public class PaginatedGeneralFeesResponse
{
	public List<GeneralFeeViewDto> Items { get; set; } = [];
	public int TotalCount { get; set; }
}