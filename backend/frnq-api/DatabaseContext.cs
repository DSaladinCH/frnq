namespace DSaladin.Frnq.Api;

using Microsoft.EntityFrameworkCore;
using DSaladin.Frnq.Api.Investment;
using DSaladin.Frnq.Api.Quote;
using DSaladin.Frnq.Api.Auth;
using DSaladin.Frnq.Api.Group;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

    public DbSet<QuoteModel> Quotes { get; set; } = null!;
    public DbSet<QuotePrice> QuotePrices { get; set; } = null!;
    public DbSet<QuoteGroup> QuoteGroups { get; set; } = null!;
    public DbSet<QuoteGroupMapping> QuoteGroupMappings { get; set; } = null!;
    public DbSet<QuoteName> QuoteNames { get; set; } = null!;
    public DbSet<InvestmentModel> Investments { get; set; } = null!;
    public DbSet<UserModel> Users { get; set; } = null!;
    public DbSet<RefreshTokenSession> RefreshTokenSessions { get; set; } = null!;
    public DbSet<OidcProvider> OidcProviders { get; set; } = null!;
    public DbSet<OidcState> OidcStates { get; set; } = null!;
    public DbSet<ExternalUserLink> ExternalUserLinks { get; set; } = null!;

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

        modelBuilder.Entity<QuoteGroup>()
            .HasOne(qg => qg.User)
            .WithMany()
            .HasForeignKey(qg => qg.UserId);

        modelBuilder.Entity<QuoteGroupMapping>()
            .HasKey(qgm => new { qgm.UserId, qgm.QuoteId });

        modelBuilder.Entity<QuoteGroupMapping>()
            .HasOne(qgm => qgm.User)
            .WithMany()
            .HasForeignKey(qgm => qgm.UserId);

        modelBuilder.Entity<QuoteGroupMapping>()
            .HasOne(qgm => qgm.Quote)
            .WithMany(q => q.Mappings)
            .HasForeignKey(qgm => qgm.QuoteId);

        modelBuilder.Entity<QuoteGroupMapping>()
            .HasOne(qgm => qgm.Group)
            .WithMany(g => g.Mappings)
            .HasForeignKey(qgm => qgm.GroupId);

        modelBuilder.Entity<QuoteName>()
            .HasKey(qn => new { qn.UserId, qn.QuoteId });

        modelBuilder.Entity<QuoteName>()
            .HasOne(qn => qn.Quote)
            .WithMany(q => q.Names)
            .HasForeignKey(qn => qn.QuoteId);

        // OIDC Provider configuration
        modelBuilder.Entity<OidcProvider>()
            .HasIndex(p => p.ProviderId)
            .IsUnique();

        // External user links
        modelBuilder.Entity<ExternalUserLink>()
            .HasOne(eul => eul.User)
            .WithMany(u => u.ExternalLinks)
            .HasForeignKey(eul => eul.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExternalUserLink>()
            .HasOne(eul => eul.Provider)
            .WithMany()
            .HasForeignKey(eul => eul.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        // OIDC state
        modelBuilder.Entity<OidcState>()
            .HasOne(os => os.Provider)
            .WithMany()
            .HasForeignKey(os => os.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OidcState>()
            .HasIndex(os => os.State)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}