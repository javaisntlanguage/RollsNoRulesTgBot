using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class _40 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.DropIndex(
	        name: "IX_Products_Photo",
	        table: "Products");
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.CreateIndex(
	            name: "IX_Products_Photo",
	            table: "Products",
	            column: "Photo");
		}
    }
}
