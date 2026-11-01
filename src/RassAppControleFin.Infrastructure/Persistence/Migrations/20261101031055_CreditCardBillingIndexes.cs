using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreditCardBillingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CardMovements_TenantId_UserId_PurchaseId",
                schema: "app",
                table: "CardMovements",
                columns: new[] { "TenantId", "UserId", "PurchaseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CardMovements_TenantId_UserId_PurchaseId",
                schema: "app",
                table: "CardMovements");
        }
    }
}
