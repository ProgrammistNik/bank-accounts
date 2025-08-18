using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bank_accounts.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxAndInboxDeadLetter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InboxDeadLetters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Meta_Version = table.Column<string>(type: "text", nullable: false),
                    Meta_Source = table.Column<string>(type: "text", nullable: false),
                    Meta_CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Meta_CausationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxDeadLetters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inboxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Meta_Version = table.Column<string>(type: "text", nullable: false),
                    Meta_Source = table.Column<string>(type: "text", nullable: false),
                    Meta_CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Meta_CausationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inboxes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxDeadLetters");

            migrationBuilder.DropTable(
                name: "Inboxes");
        }
    }
}
