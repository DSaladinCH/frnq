using System.Collections.Generic;
using DSaladin.Frnq.Api.Position;
using DSaladin.Frnq.Api.Quote;

namespace DSaladin.Frnq.Api.Position;

public class PositionsResponse
{
    public List<PositionSnapshot> Snapshots { get; set; } = [];
    public List<QuoteModel> Quotes { get; set; } = [];
}
