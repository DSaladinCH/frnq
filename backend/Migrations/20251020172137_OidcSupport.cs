using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSaladin.Frnq.Api.Migrations
{
	/// <inheritdoc />
	public partial class OidcSupport : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<string>(
				name: "PasswordHash",
				table: "user",
				type: "text",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "text");

			migrationBuilder.AddColumn<bool>(
				name: "IsOidcUser",
				table: "user",
				type: "boolean",
				nullable: false,
				defaultValue: false);

			migrationBuilder.CreateTable(
				name: "oidc_provider",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					ProviderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
					DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
					AuthorizationEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
					TokenEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
					UserInfoEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
					ClientId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
					ClientSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
					Scopes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
					IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
					DisplayOrder = table.Column<int>(type: "integer", nullable: false),
					IconClass = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
					AutoRedirect = table.Column<bool>(type: "boolean", nullable: false),
					ClaimMappings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_oidc_provider", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "external_user_link",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					UserId = table.Column<Guid>(type: "uuid", nullable: false),
					ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
					ProviderUserId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
					ProviderEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_external_user_link", x => x.Id);
					table.ForeignKey(
						name: "FK_external_user_link_oidc_provider_ProviderId",
						column: x => x.ProviderId,
						principalTable: "oidc_provider",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_external_user_link_user_UserId",
						column: x => x.UserId,
						principalTable: "user",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "oidc_state",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					State = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
					ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
					Nonce = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
					ReturnUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					IsUsed = table.Column<bool>(type: "boolean", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_oidc_state", x => x.Id);
					table.ForeignKey(
						name: "FK_oidc_state_oidc_provider_ProviderId",
						column: x => x.ProviderId,
						principalTable: "oidc_provider",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_external_user_link_ProviderId_ProviderUserId",
				table: "external_user_link",
				columns: new[] { "ProviderId", "ProviderUserId" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_external_user_link_UserId",
				table: "external_user_link",
				column: "UserId");

			migrationBuilder.CreateIndex(
				name: "IX_oidc_provider_ProviderId",
				table: "oidc_provider",
				column: "ProviderId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_oidc_state_ProviderId",
				table: "oidc_state",
				column: "ProviderId");

			migrationBuilder.CreateIndex(
				name: "IX_oidc_state_State",
				table: "oidc_state",
				column: "State",
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "external_user_link");

			migrationBuilder.DropTable(
				name: "oidc_state");

			migrationBuilder.DropTable(
				name: "oidc_provider");

			migrationBuilder.DropColumn(
				name: "IsOidcUser",
				table: "user");

			migrationBuilder.AlterColumn<string>(
				name: "PasswordHash",
				table: "user",
				type: "text",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "text",
				oldNullable: true);
		}
	}
}
