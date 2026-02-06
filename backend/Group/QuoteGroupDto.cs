using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DSaladin.Frnq.Api.Validation;

namespace DSaladin.Frnq.Api.Group;

public class QuoteGroupDto
{
	private string name = string.Empty;

	[RequiredField]
	[StringLengthRange(1, 100)]
	public required string Name
	{
		get => name;
		set => name = value.Trim();
	}

	[JsonConstructor]
	private QuoteGroupDto() { }

	[SetsRequiredMembers]
	public QuoteGroupDto(string name) => Name = name;
}