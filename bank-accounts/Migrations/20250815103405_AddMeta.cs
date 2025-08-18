using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bank_accounts.Migrations
{
    /// <inheritdoc />
    public partial class AddMeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Meta_CausationId",
                table: "Outboxes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Meta_CorrelationId",
                table: "Outboxes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Meta_Source",
                table: "Outboxes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Meta_Version",
                table: "Outboxes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Meta_CausationId",
                table: "Outboxes");

            migrationBuilder.DropColumn(
                name: "Meta_CorrelationId",
                table: "Outboxes");

            migrationBuilder.DropColumn(
                name: "Meta_Source",
                table: "Outboxes");

            migrationBuilder.DropColumn(
                name: "Meta_Version",
                table: "Outboxes");
        }
    }
}
