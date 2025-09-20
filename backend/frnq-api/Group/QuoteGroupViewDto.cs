namespace DSaladin.Frnq.Api.Group;

public class QuoteGroupViewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public static QuoteGroupViewDto FromModel(QuoteGroup group)
    {
        return new QuoteGroupViewDto
        {
            Id = group.Id,
            Name = group.Name
        };
    }

    public static List<QuoteGroupViewDto> FromModelList(List<QuoteGroup> groups)
    {
        return [.. groups.Select(FromModel)];
    }
}