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

	private async Task<UserModel?> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
	{
		// Disable warning, as other solutions cause issues in postgres and memory db
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
		return await databaseContext.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
	}

	public async Task<UserModel?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
	{
		return await databaseContext.Users.FindAsync([userId], cancellationToken);
	}

	public async Task<ApiResponse<UserViewDto>> GetUserAsync(CancellationToken cancellationToken)
	{
		Guid userId = GetCurrentUserId();
		UserModel? user = await databaseContext.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

		if (user is null)
			return ApiResponses.Unauthorized401;

		return ApiResponse.Create(new UserViewDto
		{
			UserId = user.Id,
			Email = user.Email,
			Name = user.Firstname,
			DateFormat = user.DateFormat.ToString().ToLower()
		}, System.Net.HttpStatusCode.OK);
	}

	public async Task<ApiResponse> UpdateUserAsync(UserDto userDto, CancellationToken cancellationToken)
	{
		Guid userId = GetCurrentUserId();
		UserModel? user = await GetUserByIdAsync(userId, cancellationToken);

		if (user is null)
			return ApiResponse.Create("USER_NOT_FOUND", "User not found", System.Net.HttpStatusCode.NotFound);

		user.DateFormat = userDto.DateFormat;

		await databaseContext.SaveChangesAsync(cancellationToken);
		return ApiResponse.Create("SUCCESS", "User updated successfully", System.Net.HttpStatusCode.OK);
	}

	public async Task<ApiResponse> SignupUserAsync(SignupModel signup, CancellationToken cancellationToken)
	{
		// Check if signup is enabled
		bool signupEnabled = configuration.GetValue("Features:SignupEnabled", true);

		if (!signupEnabled)
			return ApiResponse.Create(ResponseCodes.Signup.SignupDisabled, System.Net.HttpStatusCode.Forbidden);

		// Validate required fields
		if (string.IsNullOrWhiteSpace(signup.Email))
			return ApiResponse.Create(ResponseCodes.Signup.EmailRequired, System.Net.HttpStatusCode.BadRequest);

		if (string.IsNullOrWhiteSpace(signup.Password))
			return ApiResponse.Create(ResponseCodes.Signup.PasswordRequired, System.Net.HttpStatusCode.BadRequest);

		if (string.IsNullOrWhiteSpace(signup.Firstname))
			return ApiResponse.Create(ResponseCodes.Signup.FirstnameRequired, System.Net.HttpStatusCode.BadRequest);

		// Check if user already exists
		if (await GetUserByEmailAsync(signup.Email, cancellationToken) != null)
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

		await databaseContext.Users.AddAsync(user, cancellationToken);
		await databaseContext.SaveChangesAsync(cancellationToken);

		return ApiResponses.Created201;
	}

	public async Task<ApiResponse<LoginResponseModel>> LoginUserAsync(LoginModel login, CancellationToken cancellationToken)
	{
		// Validate required fields
		if (string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
			return ApiResponse.Create(ResponseCodes.Login.UserInvalid, System.Net.HttpStatusCode.Unauthorized);

		UserModel? user = await GetUserByEmailAsync(login.Email, cancellationToken);

		if (user is null || !BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
			return ApiResponse.Create(ResponseCodes.Login.UserInvalid, System.Net.HttpStatusCode.Unauthorized);

		return await LoginUserByUserModelAsync(user, cancellationToken);
	}

	/// <summary>
	/// Internal method to log in a user that has already been authenticated (used by both password and OIDC login)
	/// </summary>
	public async Task<ApiResponse<LoginResponseModel>> LoginUserByUserModelAsync(UserModel user, CancellationToken cancellationToken)
	{
		string accessToken = GenerateAccessToken(user);
		string refreshToken = GenerateRefreshToken();
		DateTime expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(configuration.GetSection("JwtSettings")["AccessTokenExpiryInMinutes"]!));

		// Create refresh token session
		var refreshTokenSession = new RefreshTokenSession
		{
			UserId = user.Id,
			Token = refreshToken,
			ExpiryTime = DateTime.UtcNow.AddDays(7), // 7 days expiry
			DeviceInfo = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
			IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
		};

		await databaseContext.RefreshTokenSessions.AddAsync(refreshTokenSession, cancellationToken);
		await databaseContext.SaveChangesAsync(cancellationToken);

		// Set refresh token as HTTP-only cookie
		var cookieOptions = new CookieOptions
		{
			HttpOnly = true,
			Expires = refreshTokenSession.ExpiryTime,
			SameSite = SameSiteMode.None,
			Secure = true,
#if !DEBUG
            Domain = configuration.GetValue<string>("Cookies:Domain"),
#endif
		};

		httpContextAccessor.HttpContext?.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

		var loginResponse = new LoginResponseModel
		{
			AccessToken = accessToken,
			ExpiresAt = expiresAt
		};

		return ApiResponse.Create(loginResponse, System.Net.HttpStatusCode.OK);
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
		IConfigurationSection jwtSettings = configuration.GetSection("JwtSettings");
		byte[] key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

		Claim[] claims = new[]
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
		SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
		return tokenHandler.WriteToken(token);
	}

	private static string GenerateRefreshToken()
	{
		byte[] randomNumber = new byte[64];
		using var rng = RandomNumberGenerator.Create();
		rng.GetBytes(randomNumber);
		return Convert.ToBase64String(randomNumber);
	}

	public async Task<ApiResponse<LoginResponseModel>> RefreshAccessTokenAsync(CancellationToken cancellationToken)
	{
		string? refreshToken = httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];

		if (string.IsNullOrEmpty(refreshToken))
			return ApiResponse.Create(ResponseCodes.Login.UserInvalid, System.Net.HttpStatusCode.Unauthorized);

		RefreshTokenSession? tokenSession = await databaseContext.RefreshTokenSessions
			.Include(rts => rts.User)
			.AsNoTracking()
			.FirstOrDefaultAsync(rts => rts.Token == refreshToken && rts.IsActive && rts.ExpiryTime > DateTime.UtcNow, cancellationToken);

		if (tokenSession == null)
			return ApiResponse.Create(ResponseCodes.Login.UserInvalid, System.Net.HttpStatusCode.Unauthorized);

		// Generate new access token
		string accessToken = GenerateAccessToken(tokenSession.User);
		DateTime expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(configuration.GetSection("JwtSettings")["AccessTokenExpiryInMinutes"]!));

		var loginResponse = new LoginResponseModel
		{
			AccessToken = accessToken,
			ExpiresAt = expiresAt
		};

		return ApiResponse.Create(loginResponse, System.Net.HttpStatusCode.OK);
	}

	public async Task<ApiResponse> LogoutAsync(CancellationToken cancellationToken)
	{
		string? refreshToken = httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];

		if (!string.IsNullOrEmpty(refreshToken))
		{
			RefreshTokenSession? tokenSession = await databaseContext.RefreshTokenSessions
				.FirstOrDefaultAsync(rts => rts.Token == refreshToken && rts.IsActive, cancellationToken);

			if (tokenSession != null)
			{
				tokenSession.IsActive = false;
				await databaseContext.SaveChangesAsync(cancellationToken);
			}
		}

		// Remove the refresh token cookie
		httpContextAccessor.HttpContext?.Response.Cookies.Delete("refreshToken");

		return ApiResponse.Create("SUCCESS", "Logged out successfully", System.Net.HttpStatusCode.OK);
	}
}