using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSaladin.Frnq.Api.Migrations
{
	/// <inheritdoc />
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public partial class EnumToString : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				ALTER TABLE "user"
				ALTER COLUMN "DateFormat" TYPE text
				USING CASE "DateFormat"
					WHEN 0 THEN 'English'
					WHEN 1 THEN 'German'
					ELSE 'English'
				END;
			""");

			migrationBuilder.Sql("""
				ALTER TABLE "investment"
				ALTER COLUMN "Type" TYPE text
				USING CASE "Type"
					WHEN 0 THEN 'Buy'
					WHEN 1 THEN 'Sell'
					WHEN 2 THEN 'Dividend'
					ELSE 'Buy'
				END;
			""");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				ALTER TABLE "user"
				ALTER COLUMN "DateFormat" TYPE integer
				USING CASE "DateFormat"
					WHEN 'English' THEN 0
					WHEN 'German' THEN 1
					ELSE 0
				END;
			""");

			migrationBuilder.Sql("""
				ALTER TABLE "investment"
				ALTER COLUMN "Type" TYPE integer
				USING CASE "Type"
					WHEN 'Buy' THEN 0
					WHEN 'Sell' THEN 1
					WHEN 'Dividend' THEN 2
					ELSE 0
				END;
			""");
		}
	}
}
