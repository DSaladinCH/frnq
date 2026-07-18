using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSaladin.Frnq.Api.Migrations
{
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class AddUserNumberFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NumberFormat",
                table: "user",
                type: "text",
                nullable: false,
                defaultValue: "English");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberFormat",
                table: "user");
        }
    }
}
