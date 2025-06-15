namespace DSaladin.Frnq.Api;

using Microsoft.EntityFrameworkCore;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

    public DbSet<QuoteModel> Quotes { get; set; } = null!;
    public DbSet<QuotePrice> QuotePrices { get; set; } = null!;
    public DbSet<InvestmentModel> Investments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuoteModel>()
            .HasMany(q => q.Prices)
            .WithOne(qp => qp.Quote);

        modelBuilder.Entity<QuotePrice>()
            .HasOne(qp => qp.Quote)
            .WithMany()
            .HasForeignKey(qp => new { qp.ProviderId, qp.Symbol });

        modelBuilder.Entity<InvestmentModel>()
            .HasOne(q => q.Quote)
            .WithMany()
            .HasForeignKey(q => new { q.ProviderId, q.QuoteSymbol });

        base.OnModelCreating(modelBuilder);
    }
}