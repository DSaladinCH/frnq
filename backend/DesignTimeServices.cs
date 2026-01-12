using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;

namespace DSaladin.Frnq.Api.Database;

[ExcludeFromCodeCoverage]
public sealed class DesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IMigrationsCodeGenerator, CoverageExcludedMigrationsGenerator>();
    }
}
