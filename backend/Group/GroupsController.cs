using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Group;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController(GroupManagement groupManagement) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> GetGroups()
	{
		return await groupManagement.GetAllGroupsAsync();
	}

	[HttpPost]
	public async Task<IActionResult> CreateGroup([FromBody] QuoteGroupDto quoteGroup)
	{
		return await groupManagement.CreateGroupAsync(quoteGroup);
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> UpdateGroup(int id, [FromBody] QuoteGroupDto quoteGroup)
	{
		return await groupManagement.UpdateGroupAsync(id, quoteGroup);
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> DeleteGroup(int id)
	{
		return await groupManagement.DeleteGroupAsync(id);
	}

	[HttpPost("{id}/{quoteId}")]
	public async Task<IActionResult> AddQuoteToGroup(int id, int quoteId)
	{
		return await groupManagement.AddQuoteToGroupAsync(id, quoteId);
	}

	[HttpDelete("{id}/{quoteId}")]
	public async Task<IActionResult> RemoveQuoteFromGroup(int id, int quoteId)
	{
		return await groupManagement.RemoveQuoteFromGroupAsync(id, quoteId);
	}
}