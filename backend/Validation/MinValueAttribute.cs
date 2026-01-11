using System.ComponentModel.DataAnnotations;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Validation;

/// <summary>
/// Validates that a numeric value is greater than or equal to a specified minimum.
/// </summary>
public class MinValueAttribute : ResponseCodeValidationAttribute
{
	public double Minimum { get; set; }
	public bool Inclusive { get; set; } = true;

	public MinValueAttribute(double minimum, bool inclusive = true)
	{
		Minimum = minimum;
		Inclusive = inclusive;
		ResponseCode = CodeDescriptionModel.InvalidFields;
	}

	protected override ValidationResult? IsValidCore(object? value, ValidationContext validationContext)
	{
		if (!double.TryParse(value?.ToString(), out double numericValue))  
            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));  

        if (Inclusive && numericValue < Minimum)  
            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));  

        if (!Inclusive && numericValue <= Minimum)  
            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName)); 

		return ValidationResult.Success;
	}
}
