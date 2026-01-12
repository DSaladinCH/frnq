using System.Diagnostics.CodeAnalysis;

namespace DSaladin.Frnq.Api.Result;

[ExcludeFromCodeCoverage]
public static class ResponseCodes
{
	public static class Login
	{
		public static readonly CodeDescriptionModel UserInvalid = new("USER_INVALID", "Invalid username or password.");
	}

	public static class Signup
	{
		public static readonly CodeDescriptionModel EmailRequired = new("EMAIL_REQUIRED", "Email is required.");
		public static readonly CodeDescriptionModel PasswordRequired = new("PASSWORD_REQUIRED", "Password is required.");
		public static readonly CodeDescriptionModel FirstnameRequired = new("FIRSTNAME_REQUIRED", "First name is required.");
		public static readonly CodeDescriptionModel PasswordWeak = new("PASSWORD_WEAK", "The password must be at least 12 characters long and contain uppercase, lowercase, digit, and special character.");
		public static readonly CodeDescriptionModel SignupDisabled = new("SIGNUP_DISABLED", "New user registrations are currently disabled.");
	}

	public static class Quote
	{
		public static readonly CodeDescriptionModel ProviderNotFound = new("PROVIDER_NOT_FOUND", "The requested provider was not found.");
		public static readonly CodeDescriptionModel InvalidDateRange = new("INVALID_DATE_RANGE", "The 'from' date cannot be later than the 'to' date.");
		public static readonly CodeDescriptionModel SymbolNotFound = new("SYMBOL_NOT_FOUND", "The requested symbol was not found.");
	}
}
