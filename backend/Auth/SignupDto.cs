using DSaladin.Frnq.Api.Validation;

namespace DSaladin.Frnq.Api.Auth;

public class SignupDto
{
    [RequiredField]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [RequiredField]
    [StringLengthRange(12, 100)]
    public string Password { get; set; } = string.Empty;

    [RequiredField]
    [StringLengthRange(1, 100)]
    public string Firstname { get; set; } = string.Empty;
}