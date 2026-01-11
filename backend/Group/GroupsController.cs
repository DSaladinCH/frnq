using DSaladin.Frnq.Api.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Group;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController(GroupManagement groupManagement) : ControllerBase
{
	[HttpGet]
	[ProducesResponseType(typeof(List<QuoteGroupViewDto>), StatusCodes.Status200OK)]
	public async Task<ApiResponse> GetGroups(CancellationToken cancellationToken)
	{
		return await groupManagement.GetAllGroupsAsync(cancellationToken);
	}

	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status409Conflict)]
	public async Task<ApiResponse> CreateGroup([FromBody] QuoteGroupDto quoteGroup, CancellationToken cancellationToken)
	{
		return await groupManagement.CreateGroupAsync(quoteGroup, cancellationToken);
	}

	[HttpPut("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status409Conflict)]
	public async Task<ApiResponse> UpdateGroup(int id, [FromBody] QuoteGroupDto quoteGroup, CancellationToken cancellationToken)
	{
		return await groupManagement.UpdateGroupAsync(id, quoteGroup, cancellationToken);
	}

	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status404NotFound)]
	public async Task<ApiResponse> DeleteGroup(int id, CancellationToken cancellationToken)
	{
		return await groupManagement.DeleteGroupAsync(id, cancellationToken);
	}

	[HttpPost("{id}/{quoteId}")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status404NotFound)]
	public async Task<ApiResponse> AddQuoteToGroup(int id, int quoteId, CancellationToken cancellationToken)
	{
		return await groupManagement.AddQuoteToGroupAsync(id, quoteId, cancellationToken);
	}

	[HttpDelete("{id}/{quoteId}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(CodeDescriptionModel), StatusCodes.Status404NotFound)]
	public async Task<ApiResponse> RemoveQuoteFromGroup(int id, int quoteId, CancellationToken cancellationToken)
	{
		return await groupManagement.RemoveQuoteFromGroupAsync(id, quoteId, cancellationToken);
	}
}