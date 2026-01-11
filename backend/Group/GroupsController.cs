using DSaladin.Frnq.Api.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Group;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController(GroupManagement groupManagement) : ControllerBase
{
	[HttpGet]
	public async Task<ApiResponse> GetGroups(CancellationToken cancellationToken)
	{
		return await groupManagement.GetAllGroupsAsync(cancellationToken);
	}

	[HttpPost]
	public async Task<ApiResponse> CreateGroup([FromBody] QuoteGroupDto quoteGroup, CancellationToken cancellationToken)
	{
		return await groupManagement.CreateGroupAsync(quoteGroup, cancellationToken);
	}

	[HttpPut("{id}")]
	public async Task<ApiResponse> UpdateGroup(int id, [FromBody] QuoteGroupDto quoteGroup, CancellationToken cancellationToken)
	{
		return await groupManagement.UpdateGroupAsync(id, quoteGroup, cancellationToken);
	}

	[HttpDelete("{id}")]
	public async Task<ApiResponse> DeleteGroup(int id, CancellationToken cancellationToken)
	{
		return await groupManagement.DeleteGroupAsync(id, cancellationToken);
	}

	[HttpPost("{id}/{quoteId}")]
	public async Task<ApiResponse> AddQuoteToGroup(int id, int quoteId, CancellationToken cancellationToken)
	{
		return await groupManagement.AddQuoteToGroupAsync(id, quoteId, cancellationToken);
	}

	[HttpDelete("{id}/{quoteId}")]
	public async Task<ApiResponse> RemoveQuoteFromGroup(int id, int quoteId, CancellationToken cancellationToken)
	{
		return await groupManagement.RemoveQuoteFromGroupAsync(id, quoteId, cancellationToken);
	}
}