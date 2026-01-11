using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Group;

public class GroupManagement(DatabaseContext databaseContext, AuthManagement authManagement)
{
	private readonly Guid userId = authManagement.GetCurrentUserId();

	public async Task<ApiResponse<List<QuoteGroupViewDto>>> GetAllGroupsAsync(CancellationToken cancellationToken)
	{
		return ApiResponse.Create(QuoteGroupViewDto.FromModelList(await databaseContext.QuoteGroups.Where(qg => qg.UserId == userId).ToListAsync(cancellationToken)), System.Net.HttpStatusCode.OK);
	}

	public async Task<ApiResponse> CreateGroupAsync(QuoteGroupDto quoteGroup, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(quoteGroup.Name))
			return ApiResponses.EmptyFields400;

		if (await databaseContext.QuoteGroups.AnyAsync(g => g.UserId == userId && g.Name == quoteGroup.Name, cancellationToken))
			return ApiResponses.Conflict409;

		QuoteGroup newGroup = new() { Name = quoteGroup.Name, UserId = userId };

		await databaseContext.QuoteGroups.AddAsync(newGroup, cancellationToken);
		await databaseContext.SaveChangesAsync(cancellationToken);

		return ApiResponses.Created201;
	}

	public async Task<ApiResponse> UpdateGroupAsync(int groupId, QuoteGroupDto quoteGroup, CancellationToken cancellationToken)
	{
		QuoteGroup? group = await databaseContext.QuoteGroups.FirstOrDefaultAsync(g => g.UserId == userId && g.Id == groupId, cancellationToken);

		if (group is null)
			return ApiResponses.NotFound404;

		if (string.IsNullOrWhiteSpace(quoteGroup.Name))
			return ApiResponses.EmptyFields400;

		if (await databaseContext.QuoteGroups.AnyAsync(g => g.UserId == userId && g.Name == quoteGroup.Name && g.Id != groupId, cancellationToken))
			return ApiResponses.Conflict409;

		group.Name = quoteGroup.Name;
		databaseContext.QuoteGroups.Update(group);
		await databaseContext.SaveChangesAsync(cancellationToken);

		return ApiResponse.Create("", "", System.Net.HttpStatusCode.OK);
	}

	public async Task<ApiResponse> DeleteGroupAsync(int groupId, CancellationToken cancellationToken)
	{
		QuoteGroup? group = await databaseContext.QuoteGroups.FirstOrDefaultAsync(g => g.UserId == userId && g.Id == groupId, cancellationToken);

		if (group is null)
			return ApiResponses.NotFound404;

		databaseContext.QuoteGroups.Remove(group);
		await databaseContext.SaveChangesAsync(cancellationToken);

		return ApiResponse.Create("DELETED", "", System.Net.HttpStatusCode.OK);
	}

	public async Task<ApiResponse> AddQuoteToGroupAsync(int groupId, int quoteId, CancellationToken cancellationToken)
	{
		QuoteGroup? group = await databaseContext.QuoteGroups.FirstOrDefaultAsync(g => g.UserId == userId && g.Id == groupId, cancellationToken);

		if (group is null)
			return ApiResponses.NotFound404;

		if (!await databaseContext.Quotes.AnyAsync(q => q.Id == quoteId, cancellationToken))
			return ApiResponses.NotFound404;

		QuoteGroupMapping? quoteGroupMapping = await databaseContext.QuoteGroupMappings.FirstOrDefaultAsync(m => m.UserId == userId && m.QuoteId == quoteId, cancellationToken);

		if (quoteGroupMapping is not null)
		{
			if (quoteGroupMapping.GroupId == groupId)
				return ApiResponses.Created201;

			databaseContext.QuoteGroupMappings.Remove(quoteGroupMapping);
		}

		QuoteGroupMapping mapping = new() { UserId = userId, QuoteId = quoteId, GroupId = groupId };

		await databaseContext.QuoteGroupMappings.AddAsync(mapping, cancellationToken);
		await databaseContext.SaveChangesAsync(cancellationToken);

		return ApiResponses.Created201;
	}

	public async Task<ApiResponse> RemoveQuoteFromGroupAsync(int groupId, int quoteId, CancellationToken cancellationToken)
	{
		QuoteGroupMapping? mapping = await databaseContext.QuoteGroupMappings.FirstOrDefaultAsync(m => m.UserId == userId && m.QuoteId == quoteId && m.GroupId == groupId, cancellationToken);

		if (mapping is null)
			return ApiResponses.NotFound404;

		databaseContext.QuoteGroupMappings.Remove(mapping);
		await databaseContext.SaveChangesAsync(cancellationToken);

		return ApiResponse.Create("DELETED", "", System.Net.HttpStatusCode.OK);
	}
}