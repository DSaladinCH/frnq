namespace DSaladin.Frnq.Api.Auth;

public class UserViewDto
{
	public required Guid UserId { get; set; }
	public required string Email { get; set; }
	public required string Name { get; set; }
	public required string DateFormat { get; set; }
}