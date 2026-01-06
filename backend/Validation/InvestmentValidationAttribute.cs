using System.ComponentModel.DataAnnotations;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Result;

namespace DSaladin.Frnq.Api.Validation;

public class InvestmentValidationAttribute : ResponseCodeValidationAttribute
{
	public InvestmentValidationAttribute()
	{
		ResponseCode = CodeDescriptionModel.InvalidFields;
	}

	protected override ValidationResult? IsValidCore(object? value, ValidationContext validationContext)
	{
		if (value is not InvestmentDto dto)
			return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

		if (dto.QuoteId <= 0 && (string.IsNullOrWhiteSpace(dto.ProviderId) || string.IsNullOrWhiteSpace(dto.QuoteSymbol)))
			return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

		return ValidationResult.Success;
	}
}