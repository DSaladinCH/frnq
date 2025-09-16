namespace DSaladin.Frnq.Api.Result;

public static class ResponseCodes
{
    public static class Login
    {
        public static readonly CodeDescriptionModel UserInvalid = new("USER_INVALID", "Invalid username or password.");
    }

    public static class Signup
    {
        public static readonly CodeDescriptionModel PasswordWeak = new("PASSWORD_WEAK", "The password is too weak.");
    }
}
