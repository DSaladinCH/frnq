using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DSaladin.Frnq.Api.Validation;

namespace DSaladin.Frnq.Api.Quote;

public class CustomNameDto
{
	[StringLengthRange(1, 100)]
	public required string CustomName { get; set; }

	[JsonConstructor]
	private CustomNameDto() { }

	[SetsRequiredMembers]
	public CustomNameDto(string customName) => CustomName = customName;
}