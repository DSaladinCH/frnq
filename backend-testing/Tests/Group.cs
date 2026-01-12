using DSaladin.Frnq.Api.Group;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Auth;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Group")]
public class Group : TestBase
{
	[Fact]
	public async Task GetGroups_WhenAuthenticated_ReturnsListOfUserGroups()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		TestResponse<List<QuoteGroupViewDto>> response = await ApiInterface.Groups.GetGroups();

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact]
	public async Task CreateGroup_WithUniqueGroupName_CreatesInDatabaseAndReturnsCreated()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		QuoteGroupDto group = new QuoteGroupDto("My Tech Portfolio");

		TestResponse response = await ApiInterface.Groups.CreateGroup(group);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		// Verify created in database
		QuoteGroup? createdGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Name == "My Tech Portfolio");
		Assert.NotNull(createdGroup);
		Assert.NotEqual(Guid.Empty, createdGroup.UserId);
	}

	[Fact]
	public async Task CreateGroup_WithDuplicateNameForSameUser_ReturnsConflict()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		QuoteGroupDto group = new QuoteGroupDto("Tech");
		await ApiInterface.Groups.CreateGroup(group);

		TestResponse response = await ApiInterface.Groups.CreateGroup(group);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

		// Verify only one group exists in database
		int groupCount = DbContext.QuoteGroups.Count(g => g.Name == "Tech");
		Assert.Equal(1, groupCount);
	}

	[Fact]
	public async Task UpdateGroup_WithNewName_ModifiesDatabaseAndReturnsSuccess()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		// Setup: Create initial group directly in database
		const string originalName = "Original Group";
		Guid userId = DataSeeder.TestUserId; // TestAuthHandler uses this
		await DbContext.QuoteGroups.AddAsync(new QuoteGroup
		{
			Id = 999,
			Name = originalName,
			UserId = userId
		});
		await DbContext.SaveChangesAsync();

		string updatedName = "Updated Group";
		QuoteGroupDto updateDto = new QuoteGroupDto(updatedName);
		TestResponse response = await ApiInterface.Groups.UpdateGroup(999, updateDto);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		// Verify updated in database
		QuoteGroup? updatedGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Id == 999);
		Assert.NotNull(updatedGroup);
		Assert.Equal(updatedName, updatedGroup.Name);
	}

	[Fact]
	public async Task DeleteGroup_WithExistingId_RemovesFromDatabaseAndReturnsSuccess()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		// Setup: Create initial group directly in database
		Guid userId = DataSeeder.TestUserId; // TestAuthHandler uses this
		await DbContext.QuoteGroups.AddAsync(new QuoteGroup
		{
			Id = 999,
			Name = "Group to Delete",
			UserId = userId
		});
		await DbContext.SaveChangesAsync();

		TestResponse response = await ApiInterface.Groups.DeleteGroup(999);

		Assert.NotNull(response);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		// Verify deleted from database
		QuoteGroup? deletedGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Id == 999);
		Assert.Null(deletedGroup);
	}

	[Fact]
	public async Task AddQuoteToGroup_WithValidIds_CreatesRelationshipInDatabase()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		// Setup: Create a test group directly in database
		Guid userId = DataSeeder.TestUserId;
		const int groupId = 888;
		const int quoteId = 1; // AAPL from DataSeeder
		await DbContext.QuoteGroups.AddAsync(new QuoteGroup
		{
			Id = groupId,
			Name = "Test Group",
			UserId = userId
		});
		await DbContext.SaveChangesAsync();

		// Add quote to group
		TestResponse addResponse = await ApiInterface.Groups.AddQuoteToGroup(groupId, quoteId);
		Assert.NotNull(addResponse);
		Assert.True(addResponse.StatusCode == HttpStatusCode.Created);

		// Verify added to database
		QuoteGroupMapping? groupQuote = DbContext.QuoteGroupMappings.FirstOrDefault(gq => gq.GroupId == groupId && gq.QuoteId == quoteId);
		Assert.NotNull(groupQuote);
	}

	[Fact]
	public async Task RemoveQuoteFromGroup_WithValidIds_RemovesRelationshipFromDatabase()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();

		// Setup: Create a test group with a quote relationship directly in database
		Guid userId = DataSeeder.TestUserId;
		const int groupId = 777;
		const int quoteId = 1; // AAPL from DataSeeder
		await DbContext.QuoteGroups.AddAsync(new QuoteGroup
		{
			Id = groupId,
			Name = "Test Group",
			UserId = userId
		});
		await DbContext.QuoteGroupMappings.AddAsync(new QuoteGroupMapping
		{
			GroupId = groupId,
			QuoteId = quoteId,
			UserId = userId
		});
		await DbContext.SaveChangesAsync();

		// Remove quote from group
		TestResponse removeResponse = await ApiInterface.Groups.RemoveQuoteFromGroup(groupId, quoteId);
		Assert.NotNull(removeResponse);
		Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);

		// Verify removed from database
		QuoteGroupMapping? groupQuote = DbContext.QuoteGroupMappings.FirstOrDefault(gq => gq.GroupId == groupId && gq.QuoteId == quoteId);
		Assert.Null(groupQuote);
	}
}
