namespace DSaladin.Frnq.Api;

using Microsoft.EntityFrameworkCore;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

    public DbSet<QuoteModel> Quotes { get; set; } = null!;
    public DbSet<QuotePrice> QuotePrices { get; set; } = null!;
    public DbSet<QuoteGroup> QuoteGroups { get; set; } = null!;
    public DbSet<InvestmentModel> Investments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuotePrice>()
            .HasOne(qp => qp.Quote)
            .WithMany(q => q.Prices)
            .HasForeignKey(qp => new { qp.ProviderId, qp.Symbol });

        modelBuilder.Entity<QuoteModel>()
            .HasOne(q => q.Group)
            .WithMany(qc => qc.Quotes)
            .HasForeignKey(q => q.GroupId);

        modelBuilder.Entity<InvestmentModel>()
            .HasOne(q => q.Quote)
            .WithMany()
            .HasForeignKey(q => new { q.ProviderId, q.QuoteSymbol });

        base.OnModelCreating(modelBuilder);
    }
}