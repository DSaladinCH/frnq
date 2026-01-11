using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Testing.Infrastructure;
using Allure.Xunit.Attributes;
using System.Net;
using System.Text.Json;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Testing.Tests;

[AllureSuite("Auth")]
public class Auth(CustomWebApplicationFactory<Program> factory) : BaseTest(factory)
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var response = await Api.Auth.Login(new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        });

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content?.AccessToken);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await Api.Auth.Login(new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        });

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSignupEnabled_ReturnsTrue()
    {
        var response = await Api.Auth.GetSignupEnabled();
        Assert.NotNull(response);
        Assert.True(response.Content);
    }

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsUser()
    {
        await AuthenticateAsync();
        var response = await Api.Auth.GetCurrentUser();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("test@example.com", response.Content?.Email);
    }

    [Fact]
    public async Task GetCurrentUser_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        Api.ClearToken();
        var response = await Api.Auth.GetCurrentUser();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signup_WithValidData_ReturnsCreated()
    {
        var signup = new SignupDto
        {
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            Firstname = "New"
        };

        var response = await Api.Auth.Signup(signup);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Signup_WithExistingEmail_ReturnsConflict()
    {
        var signup = new SignupDto
        {
            Email = "test@example.com",
            Password = "SecurePassword123!",
            Firstname = "Test"
        };

        var response = await Api.Auth.Signup(signup);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Signup_WithWeakPassword_ReturnsBadRequest()
    {
        var signup = new SignupDto
        {
            Email = "weak@example.com",
            Password = "weak",
            Firstname = "Weak"
        };

        var response = await Api.Auth.Signup(signup);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCurrentUser_WithValidData_ReturnsOk()
    {
        await AuthenticateAsync();
        var update = new UserDto
        {
            DateFormat = DateFormat.German
        };

        var response = await Api.Auth.UpdateCurrentUser(update);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify change
        var me = await Api.Auth.GetCurrentUser();
        Assert.Equal("german", me.Content?.DateFormat);
    }

    [Fact]
    public async Task RefreshToken_WithValidCookie_ReturnsNewToken()
    {
        // Login normally (bypass TestAuthHandler simulation for this)
        var loginResponse = await Api.Auth.Login(new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Extract cookie
        var cookie = loginResponse.Headers?.GetValues("Set-Cookie").FirstOrDefault(c => c.StartsWith("refreshToken="));
        Assert.NotNull(cookie);
        var cookieValue = cookie.Split(';').First();

        // Create a new request and manually add the cookie
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", cookieValue);

        var response = await HttpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<AuthResponseDto>(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(loginResult?.AccessToken);
    }

    [Fact]
    public async Task Logout_ReturnsOk()
    {
        await AuthenticateAsync();
        var response = await Api.Auth.Logout();

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
