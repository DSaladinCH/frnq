using DSaladin.Frnq.Api.Group;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Xunit;
using Allure.Xunit.Attributes;
using System.Net;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Group")]
public class Group : TestBase
{
	[Fact]
	public async Task GetUserGroups()
	{
		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();

		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse<List<QuoteGroupViewDto>> response = await ApiInterface.Groups.GetGroups();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(groupCountBefore, response.Value!.Count);
	}

	[Theory]
	[InlineData("Normal Name")] // Normal name
	[InlineData("Weird&/&%*+$# Name")] // Special chars
	[InlineData("1")] // 1 chars
	[InlineData("Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name")] // 99 chars
	public async Task CreateUserGroupValid(string name)
	{
		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		QuoteGroupDto group = new(name);
		ApiResponse response = await ApiInterface.Groups.CreateGroup(group);

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroup? createdGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Name == group.Name);
		Assert.NotNull(createdGroup);
		Assert.NotEqual(Guid.Empty, createdGroup.UserId);
		Assert.Equal(groupCountBefore + 1, DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count());
	}

	[Theory]
	[InlineData("")] // Empty name
	[InlineData("   ")] // Whitespace name
	[InlineData("Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name")] // 100+ chars
	public async Task CreateUserGroupInvalid(string name)
	{
		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		QuoteGroupDto group = new(name);
		ApiResponse response = await ApiInterface.Groups.CreateGroup(group);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroup? createdGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Name == group.Name);
		Assert.Null(createdGroup);
		Assert.Equal(groupCountBefore, DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task CreateUserGroupDuplicate()
	{
		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();
		QuoteGroup quoteGroup = DbContext.QuoteGroups.First(g => g.UserId == DataSeeder.TestUserId);

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		QuoteGroupDto group = new(quoteGroup.Name);
		ApiResponse response = await ApiInterface.Groups.CreateGroup(group);

		Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroup? createdGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Name == group.Name);
		Assert.NotNull(createdGroup);
		Assert.Equal(groupCountBefore, DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task CreateUserGroupUnauthenticated()
	{
		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();

		QuoteGroupDto group = new("Unauthorized Group");
		ApiResponse response = await ApiInterface.Groups.CreateGroup(group);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroup? createdGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Name == group.Name);
		Assert.Null(createdGroup);
		Assert.Equal(groupCountBefore, DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count());
	}

	[Theory]
	[InlineData("Normal Name")] // Normal name
	[InlineData("Weird&/&%*+$# Name")] // Special chars
	[InlineData("1")] // 1 chars
	[InlineData("Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name")] // 99 chars
	public async Task UpdateUserGroupValid(string name)
	{
		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();
		QuoteGroup quoteGroup = DbContext.QuoteGroups.First(g => g.UserId == DataSeeder.TestUserId);

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		QuoteGroupDto group = new(name);
		ApiResponse response = await ApiInterface.Groups.UpdateGroup(quoteGroup.Id, group);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroup? createdGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Name == group.Name);
		Assert.NotNull(createdGroup);
		Assert.NotEqual(Guid.Empty, createdGroup.UserId);
		Assert.Equal(groupCountBefore, DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count());
	}

	[Theory]
	[InlineData("")] // Empty name
	[InlineData("   ")] // Whitespace name
	[InlineData("Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name Long Name")] // 100+ chars
	public async Task UpdateUserGroupInvalid(string name)
	{
		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();
		QuoteGroup quoteGroup = DbContext.QuoteGroups.First(g => g.UserId == DataSeeder.TestUserId);

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		QuoteGroupDto group = new(name);
		ApiResponse response = await ApiInterface.Groups.UpdateGroup(quoteGroup.Id, group);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroup? createdGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Name == group.Name);
		Assert.Null(createdGroup);
		Assert.Equal(groupCountBefore, DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task UpdateUserGroupNotExisting()
	{
		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		QuoteGroupDto group = new("NonExistingGroupName");
		ApiResponse response = await ApiInterface.Groups.UpdateGroup(999, group);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroup? createdGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Name == group.Name);
		Assert.Null(createdGroup);
		Assert.Equal(groupCountBefore, DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task UpdateUserGroupDuplicate()
	{
		QuoteGroup quoteGroup = DbContext.QuoteGroups.First(g => g.UserId == DataSeeder.TestUserId);

		// Create another group to cause duplicate on update
		QuoteGroup existingNewGroup = new()
		{
			UserId = DataSeeder.TestUserId,
			Name = "Existing Group for Duplicate Test"
		};
		await DbContext.QuoteGroups.AddAsync(existingNewGroup);
		await DbContext.SaveChangesAsync();

		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		QuoteGroupDto group = new(existingNewGroup.Name);
		ApiResponse response = await ApiInterface.Groups.UpdateGroup(quoteGroup.Id, group);

		Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		// The original group should remain unchanged
		QuoteGroup? createdGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Name == quoteGroup.Name);
		Assert.NotNull(createdGroup);
		Assert.Equal(groupCountBefore, DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task UpdateUserGroupUnauthenticated()
	{
		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();
		QuoteGroup quoteGroup = DbContext.QuoteGroups.First(g => g.UserId == DataSeeder.TestUserId);

		QuoteGroupDto group = new("Unauthorized Group");
		ApiResponse response = await ApiInterface.Groups.UpdateGroup(quoteGroup.Id, group);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroup? createdGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Name == group.Name);
		Assert.Null(createdGroup);
		Assert.Equal(groupCountBefore, DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task DeleteUserGroupValid()
	{
		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();
		QuoteGroup quoteGroup = DbContext.QuoteGroups.First(g => g.UserId == DataSeeder.TestUserId);

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse response = await ApiInterface.Groups.DeleteGroup(quoteGroup.Id);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroup? deletedGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Id == quoteGroup.Id);
		Assert.Null(deletedGroup);
		Assert.Equal(groupCountBefore - 1, DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task DeleteUserGroupInvalid()
	{
		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();

		using AuthenticationScope<UserModel> authScope = await Authenticate();

		ApiResponse response = await ApiInterface.Groups.DeleteGroup(999);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		Assert.Equal(groupCountBefore, DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task DeleteUserGroupUnauthenticated()
	{
		int groupCountBefore = DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count();
		QuoteGroup quoteGroup = DbContext.QuoteGroups.First(g => g.UserId == DataSeeder.TestUserId);

		ApiResponse response = await ApiInterface.Groups.DeleteGroup(quoteGroup.Id);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroup? createdGroup = DbContext.QuoteGroups.FirstOrDefault(g => g.Id == quoteGroup.Id);
		Assert.NotNull(createdGroup);
		Assert.Equal(groupCountBefore, DbContext.QuoteGroups.Where(g => g.UserId == DataSeeder.TestUserId).Count());
	}

	[Fact]
	public async Task AddQuoteToGroupValid()
	{
		QuoteGroupMapping existingMapping = DbContext.QuoteGroupMappings.First(gq => gq.UserId == DataSeeder.TestUserId);

		// Remove the existing mapping so we can test adding it again
		DbContext.QuoteGroupMappings.Remove(existingMapping);
		await DbContext.SaveChangesAsync();

		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse response = await ApiInterface.Groups.AddQuoteToGroup(existingMapping.GroupId, existingMapping.QuoteId);

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroupMapping? createdMapping = DbContext.QuoteGroupMappings.FirstOrDefault(gq =>
			gq.UserId == DataSeeder.TestUserId && gq.GroupId == existingMapping.GroupId && gq.QuoteId == existingMapping.QuoteId);

		Assert.NotNull(createdMapping);
	}

	[Fact]
	public async Task AddQuoteToGroupInvalidGroup()
	{
		QuoteGroupMapping existingMapping = DbContext.QuoteGroupMappings.First(gq => gq.UserId == DataSeeder.TestUserId);

		// Remove the existing mapping so we can test adding it again
		DbContext.QuoteGroupMappings.Remove(existingMapping);
		await DbContext.SaveChangesAsync();

		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse response = await ApiInterface.Groups.AddQuoteToGroup(999, existingMapping.QuoteId);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroupMapping? createdMapping = DbContext.QuoteGroupMappings.FirstOrDefault(gq =>
			gq.UserId == DataSeeder.TestUserId && gq.GroupId == 999 && gq.QuoteId == existingMapping.QuoteId);

		Assert.Null(createdMapping);
	}

	[Fact]
	public async Task AddQuoteToGroupInvalidQuote()
	{
		QuoteGroupMapping existingMapping = DbContext.QuoteGroupMappings.First(gq => gq.UserId == DataSeeder.TestUserId);

		// Remove the existing mapping so we can test adding it again
		DbContext.QuoteGroupMappings.Remove(existingMapping);
		await DbContext.SaveChangesAsync();

		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse response = await ApiInterface.Groups.AddQuoteToGroup(existingMapping.GroupId, 999);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroupMapping? createdMapping = DbContext.QuoteGroupMappings.FirstOrDefault(gq =>
			gq.UserId == DataSeeder.TestUserId && gq.GroupId == existingMapping.GroupId && gq.QuoteId == 999);

		Assert.Null(createdMapping);
	}

	[Fact]
	public async Task AddQuoteToGroupDuplicate()
	{
		QuoteGroupMapping existingMapping = DbContext.QuoteGroupMappings.First(gq => gq.UserId == DataSeeder.TestUserId);

		// We don't remove the existing mapping here to test duplicate addition

		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse response = await ApiInterface.Groups.AddQuoteToGroup(existingMapping.GroupId, existingMapping.QuoteId);

		// Should return Created even if duplicate, per API design
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroupMapping? createdMapping = DbContext.QuoteGroupMappings.FirstOrDefault(gq =>
			gq.UserId == DataSeeder.TestUserId && gq.GroupId == existingMapping.GroupId && gq.QuoteId == existingMapping.QuoteId);

		Assert.NotNull(createdMapping);
	}

	[Fact]
	public async Task UpdateQuoteToGroupValid()
	{
		QuoteGroupMapping existingMapping = DbContext.QuoteGroupMappings.First(gq => gq.UserId == DataSeeder.TestUserId);
		QuoteGroup newGroup = new()
		{
			UserId = DataSeeder.TestUserId,
			Name = "New Group for Update Test"
		};
		await DbContext.QuoteGroups.AddAsync(newGroup);
		await DbContext.SaveChangesAsync();

		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse response = await ApiInterface.Groups.AddQuoteToGroup(newGroup.Id, existingMapping.QuoteId);

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		DbContext.ChangeTracker.Clear();

		// Old mapping should be removed
		QuoteGroupMapping? oldMapping = DbContext.QuoteGroupMappings.FirstOrDefault(gq =>
			gq.UserId == DataSeeder.TestUserId && gq.GroupId == existingMapping.GroupId && gq.QuoteId == existingMapping.QuoteId);

		Assert.Null(oldMapping);

		// New mapping should be created
		QuoteGroupMapping? createdMapping = DbContext.QuoteGroupMappings.FirstOrDefault(gq =>
			gq.UserId == DataSeeder.TestUserId && gq.GroupId == newGroup.Id && gq.QuoteId == existingMapping.QuoteId);

		Assert.NotNull(createdMapping);
	}

	[Fact]
	public async Task RemoveQuoteFromGroupValid()
	{
		QuoteGroupMapping existingMapping = DbContext.QuoteGroupMappings.First(gq => gq.UserId == DataSeeder.TestUserId);

		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse response = await ApiInterface.Groups.RemoveQuoteFromGroup(existingMapping.GroupId, existingMapping.QuoteId);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroupMapping? createdMapping = DbContext.QuoteGroupMappings.FirstOrDefault(gq =>
			gq.UserId == DataSeeder.TestUserId && gq.GroupId == existingMapping.GroupId && gq.QuoteId == existingMapping.QuoteId);

		Assert.Null(createdMapping);
	}

	[Fact]
	public async Task RemoveQuoteFromGroupInvalid()
	{
		QuoteGroupMapping existingMapping = DbContext.QuoteGroupMappings.First(gq => gq.UserId == DataSeeder.TestUserId);

		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse response = await ApiInterface.Groups.RemoveQuoteFromGroup(999, 999);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		QuoteGroupMapping? createdMapping = DbContext.QuoteGroupMappings.FirstOrDefault(gq =>
			gq.UserId == DataSeeder.TestUserId && gq.GroupId == existingMapping.GroupId && gq.QuoteId == existingMapping.QuoteId);

		Assert.NotNull(createdMapping);
	}
}
