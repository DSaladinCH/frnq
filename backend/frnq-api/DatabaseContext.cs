namespace DSaladin.Frnq.Api;

using Microsoft.EntityFrameworkCore;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Auth;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

    public DbSet<QuoteModel> Quotes { get; set; } = null!;
    public DbSet<QuotePrice> QuotePrices { get; set; } = null!;
    public DbSet<QuoteGroup> QuoteGroups { get; set; } = null!;
    public DbSet<InvestmentModel> Investments { get; set; } = null!;
    public DbSet<UserModel> Users { get; set; } = null!;
    public DbSet<RefreshTokenSession> RefreshTokenSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuoteModel>()
            .HasOne(q => q.Group)
            .WithMany(qc => qc.Quotes)
            .HasForeignKey(q => q.GroupId);

        modelBuilder.Entity<QuoteModel>()
            .HasIndex(q => new { q.ProviderId, q.Symbol })
            .IsUnique();

        modelBuilder.Entity<QuotePrice>()
            .HasOne(qp => qp.Quote)
            .WithMany(q => q.Prices)
            .HasForeignKey(qp => qp.QuoteId);

        modelBuilder.Entity<QuotePrice>()
            .HasIndex(qp => new { qp.QuoteId, qp.Date })
            .IsUnique();

        modelBuilder.Entity<InvestmentModel>()
            .HasOne(q => q.Quote)
            .WithMany()
            .HasForeignKey(q => q.QuoteId);

        base.OnModelCreating(modelBuilder);
    }
}