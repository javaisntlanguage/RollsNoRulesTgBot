using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class _36 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderCarts_Products_ProductId",
                table: "OrderCarts");

            migrationBuilder.DropIndex(
                name: "IX_OrderCarts_ProductId",
                table: "OrderCarts");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "OrderCarts");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "OrderCarts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "OrderCarts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "OrderCarts");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "OrderCarts");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "OrderCarts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OrderCarts_ProductId",
                table: "OrderCarts",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderCarts_Products_ProductId",
                table: "OrderCarts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
