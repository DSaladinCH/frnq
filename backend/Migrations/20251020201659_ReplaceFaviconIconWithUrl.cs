using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSaladin.Frnq.Api.Migrations
{
	/// <inheritdoc />
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public partial class ReplaceFaviconIconWithUrl : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "IconClass",
				table: "oidc_provider");

			migrationBuilder.AddColumn<string>(
				name: "FaviconUrl",
				table: "oidc_provider",
				type: "character varying(50000)",
				maxLength: 50000,
				nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "FaviconUrl",
				table: "oidc_provider");

			migrationBuilder.AddColumn<string>(
				name: "IconClass",
				table: "oidc_provider",
				type: "character varying(200)",
				maxLength: 200,
				nullable: true);
		}
	}
}
