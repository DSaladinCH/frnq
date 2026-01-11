using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSaladin.Frnq.Api.Migrations
{
    /// <inheritdoc />
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class AdditionalIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_investment_UserId",
                table: "investment");

            migrationBuilder.CreateIndex(
                name: "IX_investment_UserId_Date",
                table: "investment",
                columns: new[] { "UserId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_investment_UserId_Date",
                table: "investment");

            migrationBuilder.CreateIndex(
                name: "IX_investment_UserId",
                table: "investment",
                column: "UserId");
        }
    }
}
