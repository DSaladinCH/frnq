using System.ComponentModel.DataAnnotations;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Validation;

/// <summary>
/// Validates that a string length is within a specified range.
/// </summary>
public class StringLengthRangeAttribute : ResponseCodeValidationAttribute
{
    public int MinLength { get; set; }
    public int MaxLength { get; set; }

    public StringLengthRangeAttribute(int minLength, int maxLength)
    {
        MinLength = minLength;
        MaxLength = maxLength;
        ResponseCode = CodeDescriptionModel.InvalidFields;
    }

    protected override ValidationResult? IsValidCore(object? value, ValidationContext validationContext)
    {
        if (value == null || value is not string stringValue)
            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

        // Trim whitespace to match DTO behavior
        string trimmedValue = stringValue.Trim();

        if (trimmedValue.Length < MinLength || trimmedValue.Length > MaxLength)
            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

        return ValidationResult.Success;
    }
}
