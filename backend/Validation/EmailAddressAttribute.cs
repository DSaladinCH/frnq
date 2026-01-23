using System.ComponentModel.DataAnnotations;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Validation;

/// <summary>
/// Validates that a string length is within a specified range.
/// </summary>
public class EmailAddressAttribute : ResponseCodeValidationAttribute
{
	public EmailAddressAttribute() : base(CodeDescriptionModel.InvalidFields) { }

	protected override ValidationResult? IsValidCore(object? value, ValidationContext validationContext)
	{
		if (value is null)
			return ValidationResult.Success;

		if (!(value is string valueAsString))
			return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

		if (valueAsString.AsSpan().ContainsAny('\r', '\n'))
			return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

		// only return true if there is only 1 '@' character
		// and it is neither the first nor the last character
		int index = valueAsString.IndexOf('@');

		if (index > 0 && index != valueAsString.Length - 1 && index == valueAsString.LastIndexOf('@'))
			return ValidationResult.Success;

		return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
	}
}
