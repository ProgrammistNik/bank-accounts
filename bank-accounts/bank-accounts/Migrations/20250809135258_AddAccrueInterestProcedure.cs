using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bank_accounts.Migrations
{
    /// <inheritdoc />
    public partial class AddAccrueInterestProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            CREATE OR REPLACE PROCEDURE accrue_interest(account_id UUID)
            LANGUAGE plpgsql
            AS $$
            BEGIN
                UPDATE ""Accounts""
                SET ""Balance"" = ""Balance"" + (""Balance"" * (""InterestRate"" / 100) / 365)
                WHERE ""Id"" = account_id
                AND ""AccountType"" <> 0
                AND ""InterestRate"" IS NOT NULL
                AND ""ClosedAt"" IS NULL;
            END;
            $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
