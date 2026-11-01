using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RassAppControleFin.Infrastructure.Persistence.Migrations;

public partial class CreditCardBillingMovements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "InvoiceId", schema: "app", table: "CardMovements", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "PurchaseId", schema: "app", table: "CardMovements", type: "uuid", nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_CardMovements_TenantId_UserId_InvoiceId",
            schema: "app",
            table: "CardMovements",
            columns: new[] { "TenantId", "UserId", "InvoiceId" });

        migrationBuilder.AddForeignKey(
            name: "FK_CardMovements_CreditCardInvoices_TenantId_UserId_InvoiceId",
            schema: "app", table: "CardMovements",
            columns: new[] { "TenantId", "UserId", "InvoiceId" },
            principalSchema: "app", principalTable: "CreditCardInvoices",
            principalColumns: new[] { "TenantId", "UserId", "Id" }, onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_CardMovements_CreditCardPurchases_TenantId_UserId_PurchaseId",
            schema: "app", table: "CardMovements",
            columns: new[] { "TenantId", "UserId", "PurchaseId" },
            principalSchema: "app", principalTable: "CreditCardPurchases",
            principalColumns: new[] { "TenantId", "UserId", "Id" }, onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_CardMovements_CreditCardInvoices_TenantId_UserId_InvoiceId", schema: "app", table: "CardMovements");
        migrationBuilder.DropForeignKey(name: "FK_CardMovements_CreditCardPurchases_TenantId_UserId_PurchaseId", schema: "app", table: "CardMovements");
        migrationBuilder.DropIndex(name: "IX_CardMovements_TenantId_UserId_InvoiceId", schema: "app", table: "CardMovements");
        migrationBuilder.DropColumn(name: "InvoiceId", schema: "app", table: "CardMovements");
        migrationBuilder.DropColumn(name: "PurchaseId", schema: "app", table: "CardMovements");
    }
}
