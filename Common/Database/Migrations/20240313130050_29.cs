using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class _29 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SellLocationId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SellLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellLocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SellLocationId",
                table: "Orders",
                column: "SellLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_SellLocations_SellLocationId",
                table: "Orders",
                column: "SellLocationId",
                principalTable: "SellLocations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_SellLocations_SellLocationId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "SellLocations");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SellLocationId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SellLocationId",
                table: "Orders");
        }
    }
}
