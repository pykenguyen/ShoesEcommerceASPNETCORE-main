using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoesEcommerce.Migrations
{
    /// <inheritdoc />
    public partial class VnPayInvoiceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VnPayBankCode",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VnPayBankTranNo",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VnPayCardType",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VnPayTxnRef",
                table: "Invoices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VnPayBankCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VnPayBankTranNo",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VnPayCardType",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VnPayTxnRef",
                table: "Invoices");
        }
    }
}
