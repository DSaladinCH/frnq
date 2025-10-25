using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DSaladin.Frnq.Api.Migrations
{
	/// <inheritdoc />
	public partial class OidcLinkingSupport : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<Guid>(
				name: "LinkingUserId",
				table: "oidc_state",
				type: "uuid",
				nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "LinkingUserId",
				table: "oidc_state");
		}
	}
}
