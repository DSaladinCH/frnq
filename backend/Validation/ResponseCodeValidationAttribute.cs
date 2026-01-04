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

	public override string FormatErrorMessage(string name)
	{
		if (ResponseCode != null)
			return $"{ResponseCode.Code}|{ResponseCode.Description}";
			
		return base.FormatErrorMessage(name);
	}
}
