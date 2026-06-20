using DSaladin.Frnq.Api.Validation;

namespace DSaladin.Frnq.Api.GeneralFee;

public class GeneralFeeDto
{

	[MinValue(0, false)]
    public decimal Amount { get; set; }
	
	[RequiredField]
    public required DateTime Date { get; set; }

	[StringLengthRange(1, 100)]
    public string Description { get; set; } = string.Empty;

    public int? GroupId { get; set; }
}