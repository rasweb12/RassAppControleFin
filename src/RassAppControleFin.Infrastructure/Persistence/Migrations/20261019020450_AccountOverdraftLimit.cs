using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountOverdraftLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Accounts_Balance",
                schema: "app",
                table: "Accounts");

            migrationBuilder.AddColumn<decimal>(
                name: "OverdraftLimit",
                schema: "app",
                table: "Accounts",
                type: "numeric(16,2)",
                precision: 16,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Accounts_Balance",
                schema: "app",
                table: "Accounts",
                sql: "\"OverdraftLimit\" >= 0 AND \"Balance\" >= -\"OverdraftLimit\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Accounts_Balance",
                schema: "app",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "OverdraftLimit",
                schema: "app",
                table: "Accounts");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Accounts_Balance",
                schema: "app",
                table: "Accounts",
                sql: "\"Balance\" >= 0");
        }
    }
}
