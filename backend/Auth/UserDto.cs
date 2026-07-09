using DSaladin.Frnq.Api.Validation;

namespace DSaladin.Frnq.Api.Auth;

public class UserDto
{
    public required DateFormat DateFormat { get; set; }
	
	[MinValue(2)]
	public required int ForecastNumberOfInvestments { get; set; }
}
