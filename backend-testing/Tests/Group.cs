using DSaladin.Frnq.Api.Group;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Group")]
public class Group(CustomWebApplicationFactory<Program> factory) : BaseTest(factory)
{
    [Fact]
    public async Task GetGroups_WhenAuthenticated_ReturnsListOfUserGroups()
    {
        await AuthenticateAsync();
        var response = await Api.Groups.GetGroups();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroup_WithUniqueGroupName_CreatesInDatabaseAndReturnsCreated()
    {
        await AuthenticateAsync();
        var group = new QuoteGroupDto("My Tech Portfolio");

        var response = await Api.Groups.CreateGroup(group);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify created in database
        using var context = GetDatabaseContext();
        var createdGroup = context.QuoteGroups.FirstOrDefault(g => g.Name == "My Tech Portfolio");
        Assert.NotNull(createdGroup);
        Assert.NotEqual(Guid.Empty, createdGroup.UserId);
    }

    [Fact]
    public async Task CreateGroup_WithDuplicateNameForSameUser_ReturnsConflict()
    {
        await AuthenticateAsync();
        var group = new QuoteGroupDto("Tech");
        await Api.Groups.CreateGroup(group);

        var response = await Api.Groups.CreateGroup(group);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Verify only one group exists in database
        using var context = GetDatabaseContext();
        var groupCount = context.QuoteGroups.Count(g => g.Name == "Tech");
        Assert.Equal(1, groupCount);
    }

    [Fact]
    public async Task UpdateGroup_WithNewName_ModifiesDatabaseAndReturnsSuccess()
    {
        await AuthenticateAsync();
        
        // Setup: Create initial group directly in database
        const string originalName = "Original Group";
        Guid userId = DataSeeder.TestUserId; // TestAuthHandler uses this
        using (var setupContext = GetDatabaseContext())
        {
            setupContext.QuoteGroups.Add(new QuoteGroup
            {
                Id = 999,
                Name = originalName,
                UserId = userId
            });
            setupContext.SaveChanges();
        }

        var updatedName = "Updated Group";
        var updateDto = new QuoteGroupDto(updatedName);
        var response = await Api.Groups.UpdateGroup(999, updateDto);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify updated in database
        using var context = GetDatabaseContext();
        var updatedGroup = context.QuoteGroups.FirstOrDefault(g => g.Id == 999);
        Assert.NotNull(updatedGroup);
        Assert.Equal(updatedName, updatedGroup.Name);
    }

    [Fact]
    public async Task DeleteGroup_WithExistingId_RemovesFromDatabaseAndReturnsSuccess()
    {
        await AuthenticateAsync();
        
        // Setup: Create initial group directly in database
        Guid userId = DataSeeder.TestUserId; // TestAuthHandler uses this
        using (var setupContext = GetDatabaseContext())
        {
            setupContext.QuoteGroups.Add(new QuoteGroup
            {
                Id = 999,
                Name = "Group to Delete",
                UserId = userId
            });
            setupContext.SaveChanges();
        }

        var response = await Api.Groups.DeleteGroup(999);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify deleted from database
        using var context = GetDatabaseContext();
        var deletedGroup = context.QuoteGroups.FirstOrDefault(g => g.Id == 999);
        Assert.Null(deletedGroup);
    }

    [Fact]
    public async Task AddQuoteToGroup_WithValidIds_CreatesRelationshipInDatabase()
    {
        await AuthenticateAsync();
        
        // Setup: Create a test group directly in database
        Guid userId = DataSeeder.TestUserId;
        const int groupId = 888;
        const int quoteId = 1; // AAPL from DataSeeder
        using (var setupContext = GetDatabaseContext())
        {
            setupContext.QuoteGroups.Add(new QuoteGroup
            {
                Id = groupId,
                Name = "Test Group",
                UserId = userId
            });
            setupContext.SaveChanges();
        }

        // Add quote to group
        var addResponse = await Api.Groups.AddQuoteToGroup(groupId, quoteId);
        Assert.NotNull(addResponse);
        Assert.True(addResponse.StatusCode == HttpStatusCode.Created || addResponse.StatusCode == HttpStatusCode.OK);

        // Verify added to database
        using var context = GetDatabaseContext();
        var groupQuote = context.QuoteGroupMappings.FirstOrDefault(gq => gq.GroupId == groupId && gq.QuoteId == quoteId);
        Assert.NotNull(groupQuote);
    }

    [Fact]
    public async Task RemoveQuoteFromGroup_WithValidIds_RemovesRelationshipFromDatabase()
    {
        await AuthenticateAsync();
        
        // Setup: Create a test group with a quote relationship directly in database
        Guid userId = DataSeeder.TestUserId;
        const int groupId = 777;
        const int quoteId = 1; // AAPL from DataSeeder
        using (var setupContext = GetDatabaseContext())
        {
            setupContext.QuoteGroups.Add(new QuoteGroup
            {
                Id = groupId,
                Name = "Test Group",
                UserId = userId
            });
            setupContext.QuoteGroupMappings.Add(new QuoteGroupMapping
            {
                GroupId = groupId,
                QuoteId = quoteId,
                UserId = userId
            });
            setupContext.SaveChanges();
        }

        // Remove quote from group
        var removeResponse = await Api.Groups.RemoveQuoteFromGroup(groupId, quoteId);
        Assert.NotNull(removeResponse);
        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);

        // Verify removed from database
        using var context = GetDatabaseContext();
        var groupQuote = context.QuoteGroupMappings.FirstOrDefault(gq => gq.GroupId == groupId && gq.QuoteId == quoteId);
        Assert.Null(groupQuote);
    }
}
