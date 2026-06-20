using DSaladin.Frnq.Api.Auth;
using Microsoft.EntityFrameworkCore;

namespace DSaladin.Frnq.Api.GeneralFee;

public class GeneralFeeManagement(DatabaseContext databaseContext, AuthManagement authManagement)
{
    private readonly Guid userId = authManagement.GetCurrentUserId();

    /// <summary>
    /// Creates a new general fee.
    /// </summary>
    public async Task<GeneralFeeViewDto> CreateGeneralFeeAsync(
        decimal amount,
        DateTime date,
        string description,
        int? groupId = null,
        CancellationToken cancellationToken = default)
    {
        if (date > DateTime.UtcNow)
            throw new InvalidOperationException("General fees cannot be created for future dates.");

        if (amount <= 0)
            throw new InvalidOperationException("General fee amount must be positive.");

        // Validate group ownership if groupId is specified
        if (groupId.HasValue)
        {
            var group = await databaseContext.QuoteGroups
                .FirstOrDefaultAsync(g => g.Id == groupId && g.UserId == userId, cancellationToken);

            if (group == null)
                throw new InvalidOperationException("Group not found or does not belong to current user.");
        }

        var fee = new GeneralFeeModel
        {
            UserId = userId,
            Amount = amount,
            Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
            Description = description,
            GroupId = groupId,
            CreatedAt = DateTime.UtcNow
        };

        databaseContext.GeneralFees.Add(fee);
        await databaseContext.SaveChangesAsync(cancellationToken);

        return GeneralFeeViewDto.FromModel(fee);
    }

    /// <summary>
    /// Gets paginated general fees for the current user.
    /// </summary>
    public async Task<(List<GeneralFeeViewDto> Items, int TotalCount)> GetGeneralFeesForUserAsync(
        int skip = 0,
        int take = 25,
        CancellationToken cancellationToken = default)
    {
        var query = databaseContext.GeneralFees
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.Date);

        int totalCount = await query.CountAsync(cancellationToken);
        var fees = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);

        return (fees.Select(GeneralFeeViewDto.FromModel).ToList(), totalCount);
    }

    /// <summary>
    /// Gets general fees for a specific group.
    /// </summary>
    public async Task<List<GeneralFeeViewDto>> GetGeneralFeesForGroupAsync(
        int groupId,
        CancellationToken cancellationToken = default)
    {
        // Validate group ownership
        var group = await databaseContext.QuoteGroups
            .FirstOrDefaultAsync(g => g.Id == groupId && g.UserId == userId, cancellationToken);

        if (group == null)
            throw new InvalidOperationException("Group not found or does not belong to current user.");

        var fees = await databaseContext.GeneralFees
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.GroupId == groupId)
            .OrderByDescending(f => f.Date)
            .ToListAsync(cancellationToken);

        return fees.Select(GeneralFeeViewDto.FromModel).ToList();
    }

    /// <summary>
    /// Gets general fees for the current user within a date range.
    /// </summary>
    public async Task<List<GeneralFeeViewDto>> GetGeneralFeesForDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        from = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        to = DateTime.SpecifyKind(to, DateTimeKind.Utc);

        var fees = await databaseContext.GeneralFees
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.Date >= from && f.Date <= to)
            .OrderByDescending(f => f.Date)
            .ToListAsync(cancellationToken);

        return fees.Select(GeneralFeeViewDto.FromModel).ToList();
    }

    /// <summary>
    /// Gets general fees for a specific group within a date range.
    /// </summary>
    public async Task<List<GeneralFeeViewDto>> GetGeneralFeesForGroupAndDateRangeAsync(
        int groupId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        from = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        to = DateTime.SpecifyKind(to, DateTimeKind.Utc);

        var fees = await databaseContext.GeneralFees
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.GroupId == groupId && f.Date >= from && f.Date <= to)
            .OrderByDescending(f => f.Date)
            .ToListAsync(cancellationToken);

        return fees.Select(GeneralFeeViewDto.FromModel).ToList();
    }

    /// <summary>
    /// Deletes a general fee by ID.
    /// </summary>
    public async Task DeleteGeneralFeeAsync(int feeId, CancellationToken cancellationToken = default)
    {
        var fee = await databaseContext.GeneralFees
            .FirstOrDefaultAsync(f => f.Id == feeId && f.UserId == userId, cancellationToken);

        if (fee == null)
            throw new InvalidOperationException("General fee not found or does not belong to current user.");

        databaseContext.GeneralFees.Remove(fee);
        await databaseContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates an existing general fee with complete data.
    /// </summary>
    public async Task<GeneralFeeViewDto> UpdateGeneralFeeAsync(
        int feeId,
        decimal amount,
        DateTime date,
        string description,
        int? groupId,
        CancellationToken cancellationToken = default)
    {
        var fee = await databaseContext.GeneralFees
            .FirstOrDefaultAsync(f => f.Id == feeId && f.UserId == userId, cancellationToken);

        if (fee == null)
            throw new InvalidOperationException("General fee not found or does not belong to current user.");

        // Validation: can't update fee to future date
        if (date > DateTime.UtcNow)
            throw new InvalidOperationException("General fees cannot have future dates.");

        if (amount <= 0)
            throw new InvalidOperationException("General fee amount must be positive.");

        // Validate group ownership if groupId is specified and different
        if (groupId.HasValue && groupId != fee.GroupId)
        {
            var group = await databaseContext.QuoteGroups
                .FirstOrDefaultAsync(g => g.Id == groupId && g.UserId == userId, cancellationToken);

            if (group == null)
                throw new InvalidOperationException("Group not found or does not belong to current user.");
        }

        fee.Amount = amount;
        fee.Date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        fee.Description = description;
        fee.GroupId = groupId;

        databaseContext.GeneralFees.Update(fee);
        await databaseContext.SaveChangesAsync(cancellationToken);

        return GeneralFeeViewDto.FromModel(fee);
    }
}
