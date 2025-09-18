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
    public DbSet<QuoteGroupMapping> QuoteGroupMappings { get; set; } = null!;
    public DbSet<InvestmentModel> Investments { get; set; } = null!;
    public DbSet<UserModel> Users { get; set; } = null!;
    public DbSet<RefreshTokenSession> RefreshTokenSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        modelBuilder.Entity<InvestmentModel>()
            .HasOne(i => i.User)
            .WithMany(u => u.Investments)
            .HasForeignKey(i => i.UserId);

        modelBuilder.Entity<QuoteGroupMapping>()
            .HasKey(qgm => new { qgm.UserId, qgm.QuoteId });

        modelBuilder.Entity<QuoteGroupMapping>()
            .HasOne(qgm => qgm.User)
            .WithMany()
            .HasForeignKey(qgm => qgm.UserId);

        modelBuilder.Entity<QuoteGroupMapping>()
            .HasOne(qgm => qgm.Quote)
            .WithMany()
            .HasForeignKey(qgm => qgm.QuoteId);

        modelBuilder.Entity<QuoteGroupMapping>()
            .HasOne(qgm => qgm.Group)
            .WithMany()
            .HasForeignKey(qgm => qgm.GroupId);

        base.OnModelCreating(modelBuilder);
    }
}