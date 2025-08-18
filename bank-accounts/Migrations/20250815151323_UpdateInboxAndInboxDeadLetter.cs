using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bank_accounts.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInboxAndInboxDeadLetter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Payload",
                table: "Inboxes");

            migrationBuilder.DropColumn(
                name: "Payload",
                table: "InboxDeadLetters");

            migrationBuilder.AddColumn<Guid>(
                name: "Payload_OwnerId",
                table: "Inboxes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Payload_Status",
                table: "Inboxes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "Payload_OwnerId",
                table: "InboxDeadLetters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Payload_Status",
                table: "InboxDeadLetters",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Payload_OwnerId",
                table: "Inboxes");

            migrationBuilder.DropColumn(
                name: "Payload_Status",
                table: "Inboxes");

            migrationBuilder.DropColumn(
                name: "Payload_OwnerId",
                table: "InboxDeadLetters");

            migrationBuilder.DropColumn(
                name: "Payload_Status",
                table: "InboxDeadLetters");

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                table: "Inboxes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                table: "InboxDeadLetters",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
