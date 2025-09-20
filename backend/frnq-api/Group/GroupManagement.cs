using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Group;

public class GroupManagement(DatabaseContext databaseContext, AuthManagement authManagement)
{
    private readonly Guid userId = authManagement.GetCurrentUserId();

    public async Task<ApiResponse<List<QuoteGroupViewDto>>> GetAllGroupsAsync()
    {
        return ApiResponse<List<QuoteGroupViewDto>>.Create(QuoteGroupViewDto.FromModelList(await databaseContext.QuoteGroups.Where(qg => qg.UserId == userId).ToListAsync()), System.Net.HttpStatusCode.OK);
    }

    public async Task<ApiResponse> CreateGroupAsync(QuoteGroupDto quoteGroup)
    {
        if (string.IsNullOrWhiteSpace(quoteGroup.Name))
            return ApiResponses.EmptyFields400;

        if (await databaseContext.QuoteGroups.AnyAsync(g => g.UserId == userId && g.Name == quoteGroup.Name))
            return ApiResponses.Conflict409.Convert<QuoteGroupViewDto>();

        QuoteGroup newGroup = new() { Name = quoteGroup.Name, UserId = userId };

        await databaseContext.QuoteGroups.AddAsync(newGroup);
        await databaseContext.SaveChangesAsync();

        return ApiResponses.Created201;
    }

    public async Task<ApiResponse> UpdateGroupAsync(int groupId, QuoteGroupDto quoteGroup)
    {
        QuoteGroup? group = await databaseContext.QuoteGroups.FirstOrDefaultAsync(g => g.UserId == userId && g.Id == groupId);

        if (group is null)
            return ApiResponses.NotFound404;

        if (string.IsNullOrWhiteSpace(quoteGroup.Name))
            return ApiResponses.EmptyFields400;

        if (await databaseContext.QuoteGroups.AnyAsync(g => g.UserId == userId && g.Name == quoteGroup.Name && g.Id != groupId))
            return ApiResponses.Conflict409;

        group.Name = quoteGroup.Name;
        databaseContext.QuoteGroups.Update(group);
        await databaseContext.SaveChangesAsync();

        return ApiResponse.Create("", "", System.Net.HttpStatusCode.OK);
    }

    public async Task<ApiResponse> DeleteGroupAsync(int groupId)
    {
        QuoteGroup? group = await databaseContext.QuoteGroups.FirstOrDefaultAsync(g => g.UserId == userId && g.Id == groupId);

        if (group is null)
            return ApiResponses.NotFound404;

        databaseContext.QuoteGroups.Remove(group);
        await databaseContext.SaveChangesAsync();

        return ApiResponse.Create("DELETED", "", System.Net.HttpStatusCode.OK);
    }

    public async Task<ApiResponse> AddQuoteToGroupAsync(int groupId, int quoteId)
    {
        QuoteGroup? group = await databaseContext.QuoteGroups.FirstOrDefaultAsync(g => g.UserId == userId && g.Id == groupId);

        if (group is null)
            return ApiResponses.NotFound404;

        if (!await databaseContext.Quotes.AnyAsync(q => q.Id == quoteId))
            return ApiResponses.NotFound404;

        if (await databaseContext.QuoteGroupMappings.AnyAsync(m => m.UserId == userId && m.QuoteId == quoteId))
            return ApiResponses.Conflict409;

        QuoteGroupMapping mapping = new() { UserId = userId, QuoteId = quoteId, GroupId = groupId };

        await databaseContext.QuoteGroupMappings.AddAsync(mapping);
        await databaseContext.SaveChangesAsync();

        return ApiResponses.Created201;
    }

    public async Task<ApiResponse> RemoveQuoteFromGroupAsync(int groupId, int quoteId)
    {
        QuoteGroupMapping? mapping = await databaseContext.QuoteGroupMappings.FirstOrDefaultAsync(m => m.UserId == userId && m.QuoteId == quoteId && m.GroupId == groupId);

        if (mapping is null)
            return ApiResponses.NotFound404;

        databaseContext.QuoteGroupMappings.Remove(mapping);
        await databaseContext.SaveChangesAsync();

        return ApiResponse.Create("DELETED", "", System.Net.HttpStatusCode.OK);
    }
}