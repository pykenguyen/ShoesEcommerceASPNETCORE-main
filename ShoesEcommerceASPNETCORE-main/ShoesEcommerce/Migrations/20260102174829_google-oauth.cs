using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoesEcommerce.Migrations
{
    /// <inheritdoc />
    public partial class googleoauth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                table: "Customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "Customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthProvider",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "Customers");
        }
    }
}
