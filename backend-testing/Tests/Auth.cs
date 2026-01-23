using Allure.Xunit.Attributes;
using System.Net;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Auth")]
public class Auth : TestBase
{
	[Fact]
	public async Task LoginValid()
	{
		ApiResponse<AuthResponseDto> response = await ApiInterface.Auth.Login(new LoginDto
		{
			Email = DataSeeder.TestUserEmail,
			Password = DataSeeder.TestUserPassword
		});

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Value?.AccessToken);
	}

	[Fact]
	public async Task LoginWrongPassword()
	{
		ApiResponse<AuthResponseDto> response = await ApiInterface.Auth.Login(new LoginDto
		{
			Email = DataSeeder.TestUserEmail,
			Password = "WrongPassword123!"
		});

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Theory]
	[InlineData("", "")]
	[InlineData("   " , "   ")]
	[InlineData("not-a-email", "somepassword")]
	[InlineData("", "somepassword")]
	[InlineData("someemail@example.com", "")]
	public async Task LoginInvalid(string email, string password)
	{
		ApiResponse<AuthResponseDto> response = await ApiInterface.Auth.Login(new LoginDto
		{
			Email = email,
			Password = password
		});

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Theory]
	[InlineData("someemail@example.com", "WrongPassword")]
	public async Task LoginInvalidUnauthorized(string email, string password)
	{
		ApiResponse<AuthResponseDto> response = await ApiInterface.Auth.Login(new LoginDto
		{
			Email = email,
			Password = password
		});

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task GetSignupEnabled()
	{
		ApiResponse<bool> response = await ApiInterface.Auth.GetSignupEnabled();

		Assert.True(response.Value);
	}

	[Fact]
	public async Task GetCurrentUserAuthenticated()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse<UserViewDto> response = await ApiInterface.Auth.GetCurrentUser();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(DataSeeder.TestUserEmail, response.Value?.Email);
	}

	[Fact]
	public async Task GetCurrentUserUnauthenticated()
	{
		ApiResponse<UserViewDto> response = await ApiInterface.Auth.GetCurrentUser();

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Theory]
	[InlineData("newuser1@example.com", "SecurePassword123!", "New")] // Normal case
	[InlineData("newuser2@example.com", "Complex!Pass123#$%", "John")] // Complex password
	[InlineData("newuser3@example.com", "Minimum12!!!", "A")] // Minimum length password with 1 char firstname should be valid
	[InlineData("newuser4@example.com", "VeryLongPasswordWithManyCharacters123!", "VeryLongFirstnameWithManyCharacters")] // Long password and firstname
	public async Task SignupValid(string email, string password, string firstname)
	{
		SignupDto signup = new SignupDto
		{
			Email = email,
			Password = password,
			Firstname = firstname
		};

		ApiResponse response = await ApiInterface.Auth.Signup(signup);

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		UserModel? createdUser = DbContext.Users.FirstOrDefault(u => u.Email == email);
		Assert.NotNull(createdUser);
		Assert.Equal(firstname, createdUser.Firstname);
	}

	[Fact]
	public async Task SignupDuplicateEmail()
	{
		SignupDto signup = new SignupDto
		{
			Email = DataSeeder.TestUserEmail,
			Password = "SecurePassword123!",
			Firstname = "Test"
		};

		ApiResponse response = await ApiInterface.Auth.Signup(signup);

		Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
	}

	[Theory]
	[InlineData("weak1@example.com", "weak", "Test")] // Too short
	[InlineData("weak2@example.com", "", "Test")] // Empty password
	[InlineData("weak3@example.com", "       ", "Test")] // Whitespace password
	[InlineData("weak4@example.com", "12345", "Test")] // Short numeric password
	[InlineData("weak5@example.com", "password", "Test")] // Common weak password
	public async Task SignupInvalidPassword(string email, string password, string firstname)
	{
		SignupDto signup = new SignupDto
		{
			Email = email,
			Password = password,
			Firstname = firstname
		};

		ApiResponse response = await ApiInterface.Auth.Signup(signup);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		UserModel? createdUser = DbContext.Users.FirstOrDefault(u => u.Email == email);
		Assert.Null(createdUser);
	}

	[Theory]
	[InlineData("", "SecurePassword123!", "Test")] // Empty email
	[InlineData("   ", "SecurePassword123!", "Test")] // Whitespace email
	[InlineData("invalid-email", "SecurePassword123!", "Test")] // Invalid email format
	[InlineData("newuser@example.com", "SecurePassword123!", "")] // Empty firstname
	[InlineData("newuser@example.com", "SecurePassword123!", "   ")] // Whitespace firstname
	public async Task SignupInvalidData(string email, string password, string firstname)
	{
		SignupDto signup = new SignupDto
		{
			Email = email,
			Password = password,
			Firstname = firstname
		};

		ApiResponse response = await ApiInterface.Auth.Signup(signup);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		UserModel? createdUser = DbContext.Users.FirstOrDefault(u => u.Email == email);
		Assert.Null(createdUser);
	}

	[Theory]
	[InlineData(DateFormat.German, "german")] // German format
	[InlineData(DateFormat.English, "english")] // English format
	public async Task UpdateCurrentUserValid(DateFormat dateFormat, string expectedFormat)
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserDto update = new UserDto
		{
			DateFormat = dateFormat
		};

		ApiResponse response = await ApiInterface.Auth.UpdateCurrentUser(update);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		DbContext.ChangeTracker.Clear();
		ApiResponse<UserViewDto> me = await ApiInterface.Auth.GetCurrentUser();
		Assert.Equal(expectedFormat, me.Value?.DateFormat);
	}

	[Fact]
	public async Task UpdateCurrentUserUnauthenticated()
	{
		UserDto update = new UserDto
		{
			DateFormat = DateFormat.German
		};

		ApiResponse response = await ApiInterface.Auth.UpdateCurrentUser(update);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Logout()
	{
		using AuthenticationScope<UserModel> authScope = await Authenticate();
		ApiResponse response = await ApiInterface.Auth.Logout();

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}
}
