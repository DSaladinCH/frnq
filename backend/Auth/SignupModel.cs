using System.ComponentModel.DataAnnotations;

namespace DSaladin.Frnq.Api.Auth;

public class SignupModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters long")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [MinLength(1, ErrorMessage = "First name cannot be empty")]
    public string Firstname { get; set; } = string.Empty;
}