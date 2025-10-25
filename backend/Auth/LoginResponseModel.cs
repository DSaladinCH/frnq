namespace DSaladin.Frnq.Api.Auth;

public class LoginResponseModel
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}