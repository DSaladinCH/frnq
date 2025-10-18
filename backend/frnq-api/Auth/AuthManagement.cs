using DSaladin.Frnq.Api.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DSaladin.Frnq.Api.Auth;

public class AuthManagement(DatabaseContext databaseContext, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
{
    public Guid GetCurrentUserId()
    {
        string? userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : Guid.Empty;
    }

    private async Task<UserModel?> GetUserByEmailAsync(string email)
    {
        return await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
    }

    public async Task<ApiResponse> SignupUserAsync(SignupModel signup)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(signup.Email))
            return ApiResponse.Create(ResponseCodes.Signup.EmailRequired, System.Net.HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(signup.Password))
            return ApiResponse.Create(ResponseCodes.Signup.PasswordRequired, System.Net.HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(signup.Firstname))
            return ApiResponse.Create(ResponseCodes.Signup.FirstnameRequired, System.Net.HttpStatusCode.BadRequest);

        // Check if user already exists
        if (await GetUserByEmailAsync(signup.Email) != null)
            return ApiResponses.Conflict409;

        // Validate password strength
        if (!IsValidPassword(signup.Password))
            return ApiResponse.Create(ResponseCodes.Signup.PasswordWeak, System.Net.HttpStatusCode.BadRequest);

        UserModel user = new()
        {
            Email = signup.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(signup.Password),
            Firstname = signup.Firstname.Trim()
        };

        databaseContext.Users.Add(user);
        await databaseContext.SaveChangesAsync();

        return ApiResponses.Created201;
    }

    public async Task<ApiResponse<LoginResponseModel>> LoginUserAsync(LoginModel login)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
            return ApiResponse<LoginResponseModel>.Create(null, ResponseCodes.Login.UserInvalid, System.Net.HttpStatusCode.Unauthorized);

        UserModel? user = await GetUserByEmailAsync(login.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
            return ApiResponse<LoginResponseModel>.Create(null, ResponseCodes.Login.UserInvalid, System.Net.HttpStatusCode.Unauthorized);

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(configuration.GetSection("JwtSettings")["AccessTokenExpiryInMinutes"]!));

        // Create refresh token session
        var refreshTokenSession = new RefreshTokenSession
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiryTime = DateTime.UtcNow.AddDays(7), // 7 days expiry
            DeviceInfo = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        };

        databaseContext.RefreshTokenSessions.Add(refreshTokenSession);
        await databaseContext.SaveChangesAsync();

        // Set refresh token as HTTP-only cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = refreshTokenSession.ExpiryTime,
            SameSite = SameSiteMode.None,
            Secure = true,
#if !DEBUG
            Domain = ".frnq.dsaladin.dev"
#endif
        };

        httpContextAccessor.HttpContext?.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

        var loginResponse = new LoginResponseModel
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt
        };

        return ApiResponse<LoginResponseModel>.Create(loginResponse, System.Net.HttpStatusCode.OK);
    }

    /// <summary>
    /// Validates the password strength
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <returns>True if the password is valid, false otherwise</returns>
    private static bool IsValidPassword(string password)
    {
        // Check for minimum length
        if (password.Length < 12)
            return false;

        // Check for at least one uppercase letter
        if (!password.Any(char.IsUpper))
            return false;

        // Check for at least one lowercase letter
        if (!password.Any(char.IsLower))
            return false;

        // Check for at least one digit
        if (!password.Any(char.IsDigit))
            return false;

        // Check for at least one special character
        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            return false;

        return true;
    }

    private string GenerateAccessToken(UserModel user)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Firstname),
            new Claim("jti", Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenExpiryInMinutes"]!)),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<ApiResponse<LoginResponseModel>> RefreshAccessTokenAsync()
    {
        var refreshToken = httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return ApiResponse<LoginResponseModel>.Create(null, ResponseCodes.Login.UserInvalid, System.Net.HttpStatusCode.Unauthorized);

        var tokenSession = await databaseContext.RefreshTokenSessions
            .Include(rts => rts.User)
            .FirstOrDefaultAsync(rts => rts.Token == refreshToken && rts.IsActive && rts.ExpiryTime > DateTime.UtcNow);

        if (tokenSession == null)
            return ApiResponse<LoginResponseModel>.Create(null, ResponseCodes.Login.UserInvalid, System.Net.HttpStatusCode.Unauthorized);

        // Generate new access token
        var accessToken = GenerateAccessToken(tokenSession.User);
        var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(configuration.GetSection("JwtSettings")["AccessTokenExpiryInMinutes"]!));

        var loginResponse = new LoginResponseModel
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt
        };

        return ApiResponse<LoginResponseModel>.Create(loginResponse, System.Net.HttpStatusCode.OK);
    }

    public async Task<ApiResponse> LogoutAsync()
    {
        var refreshToken = httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var tokenSession = await databaseContext.RefreshTokenSessions
                .FirstOrDefaultAsync(rts => rts.Token == refreshToken && rts.IsActive);

            if (tokenSession != null)
            {
                tokenSession.IsActive = false;
                await databaseContext.SaveChangesAsync();
            }
        }

        // Remove the refresh token cookie
        httpContextAccessor.HttpContext?.Response.Cookies.Delete("refreshToken");

        return ApiResponse.Create("SUCCESS", "Logged out successfully", System.Net.HttpStatusCode.OK);
    }
}