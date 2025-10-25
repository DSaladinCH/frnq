namespace DSaladin.Frnq.Api.Auth;

public class UserViewDto()
{
	public Guid UserId { get; set; }
	public string Email { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string DateFormat { get; set; } = string.Empty;
}