using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSaladin.Frnq.Api.Migrations
{
    /// <inheritdoc />
    public partial class QuoteGroupUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "quote_group",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_quote_group_UserId",
                table: "quote_group",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_quote_group_user_UserId",
                table: "quote_group",
                column: "UserId",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quote_group_user_UserId",
                table: "quote_group");

            migrationBuilder.DropIndex(
                name: "IX_quote_group_UserId",
                table: "quote_group");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "quote_group");
        }
    }
}
