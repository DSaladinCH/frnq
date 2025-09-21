using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSaladin.Frnq.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixGroupMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quote_group_mapping_quote_QuoteModelId",
                table: "quote_group_mapping");

            migrationBuilder.DropForeignKey(
                name: "FK_quote_group_mapping_quote_group_QuoteGroupId",
                table: "quote_group_mapping");

            migrationBuilder.DropIndex(
                name: "IX_quote_group_mapping_QuoteGroupId",
                table: "quote_group_mapping");

            migrationBuilder.DropIndex(
                name: "IX_quote_group_mapping_QuoteModelId",
                table: "quote_group_mapping");

            migrationBuilder.DropColumn(
                name: "QuoteGroupId",
                table: "quote_group_mapping");

            migrationBuilder.DropColumn(
                name: "QuoteModelId",
                table: "quote_group_mapping");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuoteGroupId",
                table: "quote_group_mapping",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuoteModelId",
                table: "quote_group_mapping",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_group_mapping_QuoteGroupId",
                table: "quote_group_mapping",
                column: "QuoteGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_quote_group_mapping_QuoteModelId",
                table: "quote_group_mapping",
                column: "QuoteModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_quote_group_mapping_quote_QuoteModelId",
                table: "quote_group_mapping",
                column: "QuoteModelId",
                principalTable: "quote",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_quote_group_mapping_quote_group_QuoteGroupId",
                table: "quote_group_mapping",
                column: "QuoteGroupId",
                principalTable: "quote_group",
                principalColumn: "Id");
        }
    }
}
