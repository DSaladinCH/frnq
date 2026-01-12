using Allure.Xunit.Attributes;
using System.Net;
using System.Text.Json;
using DSaladin.Frnq.Api.Testing.Api;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Testing.Infrastructure;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Auth")]
public class Auth : TestBase
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
		TestResponse<AuthResponseDto> response = await ApiInterface.Auth.Login(new LoginDto
        {
            Email = DataSeeder.TestUserEmail,
            Password = DataSeeder.TestUserPassword
        });

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content?.AccessToken);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
		TestResponse<AuthResponseDto> response = await ApiInterface.Auth.Login(new LoginDto
        {
            Email = DataSeeder.TestUserEmail,
            Password = "WrongPassword"
        });

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSignupEnabled_ReturnsTrue()
    {
		TestResponse<bool> response = await ApiInterface.Auth.GetSignupEnabled();
        Assert.NotNull(response);
        Assert.True(response.Content);
    }

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsUser()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();
		TestResponse<UserViewDto> response = await ApiInterface.Auth.GetCurrentUser();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(DataSeeder.TestUserEmail, response.Content?.Email);
    }

    [Fact]
    public async Task GetCurrentUser_WhenNotAuthenticated_ReturnsUnauthorized()
    {
		TestResponse<UserViewDto> response = await ApiInterface.Auth.GetCurrentUser();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signup_WithValidData_ReturnsCreated()
    {
		SignupDto signup = new SignupDto
        {
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            Firstname = "New"
        };

		TestResponse response = await ApiInterface.Auth.Signup(signup);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Signup_WithExistingEmail_ReturnsConflict()
    {
		SignupDto signup = new SignupDto
        {
            Email = DataSeeder.TestUserEmail,
            Password = "SecurePassword123!",
            Firstname = "Test"
        };

		TestResponse response = await ApiInterface.Auth.Signup(signup);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Signup_WithWeakPassword_ReturnsBadRequest()
    {
		SignupDto signup = new SignupDto
        {
            Email = "weak@example.com",
            Password = "weak",
            Firstname = "Weak"
        };

		TestResponse response = await ApiInterface.Auth.Signup(signup);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCurrentUser_WithValidData_ReturnsOk()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();
		UserDto update = new UserDto
        {
            DateFormat = DateFormat.German
        };

		TestResponse response = await ApiInterface.Auth.UpdateCurrentUser(update);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		// Verify change
		TestResponse<UserViewDto> me = await ApiInterface.Auth.GetCurrentUser();
        Assert.Equal("german", me.Content?.DateFormat);
    }

    [Fact]
    public async Task RefreshToken_WithValidCookie_ReturnsNewToken()
    {
		// Login normally (bypass TestAuthHandler simulation for this)
		TestResponse<AuthResponseDto> loginResponse = await ApiInterface.Auth.Login(new LoginDto
        {
            Email = DataSeeder.TestUserEmail,
            Password = DataSeeder.TestUserPassword
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

		// Extract cookie
		string? cookie = loginResponse.Headers?.GetValues("Set-Cookie").FirstOrDefault(c => c.StartsWith("refreshToken="));
        Assert.NotNull(cookie);
		string cookieValue = cookie.Split(';').First();

        // Create a new request and manually add the cookie
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", cookieValue);

		HttpResponseMessage response = await HttpClient.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();
		AuthResponseDto? loginResult = JsonSerializer.Deserialize<AuthResponseDto>(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(loginResult?.AccessToken);
    }

    [Fact]
    public async Task Logout_ReturnsOk()
    {
        using AuthenticationScope<UserModel> authScope = await Authenticate();
		TestResponse response = await ApiInterface.Auth.Logout();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
