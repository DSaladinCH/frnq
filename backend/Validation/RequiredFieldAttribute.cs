using System.ComponentModel.DataAnnotations;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Validation;

/// <summary>
/// Validates that a required field has a value, with ResponseCode support
/// </summary>
public class RequiredFieldAttribute : ResponseCodeValidationAttribute
{
	public RequiredFieldAttribute() : base(CodeDescriptionModel.EmptyFields) { }

	protected override ValidationResult? IsValidCore(object? value, ValidationContext validationContext)
	{
		if (value == null)
			return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

		if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
			return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

		return ValidationResult.Success;
	}
}
