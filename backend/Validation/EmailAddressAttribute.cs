using System.ComponentModel.DataAnnotations;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Validation;

/// <summary>
/// Validates that a string is a well-formed email address using the built-in .NET email validation.
/// </summary>
public class EmailAddressAttribute : ResponseCodeValidationAttribute
{
	private static readonly System.ComponentModel.DataAnnotations.EmailAddressAttribute _builtInValidator = new();

	public EmailAddressAttribute() : base(CodeDescriptionModel.InvalidFields) { }

	protected override ValidationResult? IsValidCore(object? value, ValidationContext validationContext)
	{
		if (value is null)
			return ValidationResult.Success;

		if (value is not string)
			return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

		// Use the built-in .NET EmailAddressAttribute for robust validation
		bool isValid = _builtInValidator.IsValid(value);

		return isValid 
			? ValidationResult.Success 
			: new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
	}
}
