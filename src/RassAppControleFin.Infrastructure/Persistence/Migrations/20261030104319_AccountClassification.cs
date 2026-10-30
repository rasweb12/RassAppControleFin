using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludeInNetWorth",
                schema: "app",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Institution",
                schema: "app",
                table: "Accounts",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subtype",
                schema: "app",
                table: "Accounts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "app",
                table: "Accounts",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "Bank");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Accounts_Type",
                schema: "app",
                table: "Accounts",
                sql: "\"Type\" IN ('Bank','Wallet','Investment')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Accounts_Type",
                schema: "app",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "IncludeInNetWorth",
                schema: "app",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Institution",
                schema: "app",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Subtype",
                schema: "app",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "app",
                table: "Accounts");
        }
    }
}
