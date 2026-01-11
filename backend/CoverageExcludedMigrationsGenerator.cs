using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace DSaladin.Frnq.Api.Database;

[ExcludeFromCodeCoverage]
public sealed class CoverageExcludedMigrationsGenerator : CSharpMigrationsGenerator
{
    public CoverageExcludedMigrationsGenerator(MigrationsCodeGeneratorDependencies dependencies, CSharpMigrationsGeneratorDependencies csharpDependencies) : base(dependencies, csharpDependencies) { }

    public override string GenerateMigration(string? migrationNamespace, string migrationName, IReadOnlyList<MigrationOperation> upOperations, IReadOnlyList<MigrationOperation> downOperations)
    {
        string code = base.GenerateMigration(migrationNamespace, migrationName, upOperations, downOperations);
        return code.Replace($"public partial class {migrationName}", "[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\n    public partial class " + migrationName);
    }

    public override string GenerateSnapshot(string? modelSnapshotNamespace, Type contextType, string modelSnapshotName, IModel model)
    {
        string code = base.GenerateSnapshot(modelSnapshotNamespace, contextType, modelSnapshotName, model);
        return code.Replace($"partial class {modelSnapshotName} : ModelSnapshot", "[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\n    partial class " + modelSnapshotName + " : ModelSnapshot");
    }
}
