namespace DSaladin.Frnq.Api.Result;

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
    }
}
