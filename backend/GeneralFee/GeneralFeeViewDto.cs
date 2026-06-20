using System.Text.Json.Serialization;

namespace DSaladin.Frnq.Api.GeneralFee;

/// <summary>
/// Data Transfer Object for viewing a general fee.
/// </summary>
public class GeneralFeeViewDto
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? GroupId { get; set; }
    public DateTime CreatedAt { get; set; }

    [JsonConstructor]
    private GeneralFeeViewDto() { }

    public GeneralFeeViewDto(GeneralFeeModel fee)
    {
        Id = fee.Id;
        UserId = fee.UserId;
        Date = fee.Date;
        Amount = fee.Amount;
        Description = fee.Description;
        GroupId = fee.GroupId;
        CreatedAt = fee.CreatedAt;
    }

    public static GeneralFeeViewDto FromModel(GeneralFeeModel fee) => new(fee);
}
