using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DSaladin.Frnq.Api.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderId = table.Column<string>(type: "text", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ExchangeDisposition = table.Column<string>(type: "text", nullable: false),
                    TypeDisposition = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    LastUpdatedPrices = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quote_group",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_group", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Firstname = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quote_price",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuoteId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric", nullable: false),
                    Close = table.Column<decimal>(type: "numeric", nullable: false),
                    Low = table.Column<decimal>(type: "numeric", nullable: false),
                    High = table.Column<decimal>(type: "numeric", nullable: false),
                    AdjustedClose = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_price", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quote_price_quote_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "quote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "investment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    PricePerUnit = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalFees = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_investment_quote_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "quote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_investment_user_UserId",
                        column: x => x.UserId,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quote_group_mapping",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<int>(type: "integer", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    QuoteGroupId = table.Column<int>(type: "integer", nullable: true),
                    QuoteModelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_group_mapping", x => new { x.UserId, x.QuoteId });
                    table.ForeignKey(
                        name: "FK_quote_group_mapping_quote_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "quote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quote_group_mapping_quote_QuoteModelId",
                        column: x => x.QuoteModelId,
                        principalTable: "quote",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_quote_group_mapping_quote_group_GroupId",
                        column: x => x.GroupId,
                        principalTable: "quote_group",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quote_group_mapping_quote_group_QuoteGroupId",
                        column: x => x.QuoteGroupId,
                        principalTable: "quote_group",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_quote_group_mapping_user_UserId",
                        column: x => x.UserId,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token_session",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeviceInfo = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_token_session", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_token_session_user_UserId",
                        column: x => x.UserId,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_investment_QuoteId",
                table: "investment",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_investment_UserId",
                table: "investment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_quote_ProviderId_Symbol",
                table: "quote",
                columns: new[] { "ProviderId", "Symbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_group_mapping_GroupId",
                table: "quote_group_mapping",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_quote_group_mapping_QuoteGroupId",
                table: "quote_group_mapping",
                column: "QuoteGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_quote_group_mapping_QuoteId",
                table: "quote_group_mapping",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_quote_group_mapping_QuoteModelId",
                table: "quote_group_mapping",
                column: "QuoteModelId");

            migrationBuilder.CreateIndex(
                name: "IX_quote_group_mapping_UserId_QuoteId",
                table: "quote_group_mapping",
                columns: new[] { "UserId", "QuoteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_price_QuoteId_Date",
                table: "quote_price",
                columns: new[] { "QuoteId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_token_session_UserId",
                table: "refresh_token_session",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_Email",
                table: "user",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "investment");

            migrationBuilder.DropTable(
                name: "quote_group_mapping");

            migrationBuilder.DropTable(
                name: "quote_price");

            migrationBuilder.DropTable(
                name: "refresh_token_session");

            migrationBuilder.DropTable(
                name: "quote_group");

            migrationBuilder.DropTable(
                name: "quote");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
