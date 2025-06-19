using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSaladin.Frnq.Api.Migrations
{
    /// <inheritdoc />
    public partial class QuoteLastUpdatedPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "quote");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedPrices",
                table: "quote",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedPrices",
                table: "quote");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "quote",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
