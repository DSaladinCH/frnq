using System.ComponentModel.DataAnnotations;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Validation;

/// <summary>
/// Base class for validation attributes that use ResponseCodes
/// </summary>
public abstract class ResponseCodeValidationAttribute : ValidationAttribute
{
	/// <summary>
	/// The ResponseCode to use when validation fails
	/// </summary>
	public CodeDescriptionModel? ResponseCode { get; set; }

	protected ResponseCodeValidationAttribute() { }

	protected ResponseCodeValidationAttribute(CodeDescriptionModel responseCode)
	{
		ResponseCode = responseCode;
		ErrorMessage = responseCode.Description;
	}

	protected sealed override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		if (ResponseCode is null)
			throw new InvalidOperationException($"{GetType().Name} requires ResponseCode to be set");

		return IsValidCore(value, validationContext);
	}

	protected abstract ValidationResult? IsValidCore(object? value, ValidationContext validationContext);

	public override string FormatErrorMessage(string name)
		=> $"{ResponseCode!.Code}|{ResponseCode!.Description}";
}
