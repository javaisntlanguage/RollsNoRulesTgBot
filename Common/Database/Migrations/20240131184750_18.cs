using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class _18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminStates_AdminCredentials_AdminCredentialId",
                table: "AdminStates");

            migrationBuilder.DropIndex(
                name: "IX_AdminStates_AdminCredentialId",
                table: "AdminStates");

            migrationBuilder.DropColumn(
                name: "AdminCredentialId",
                table: "AdminStates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdminCredentialId",
                table: "AdminStates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AdminStates_AdminCredentialId",
                table: "AdminStates",
                column: "AdminCredentialId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminStates_AdminCredentials_AdminCredentialId",
                table: "AdminStates",
                column: "AdminCredentialId",
                principalTable: "AdminCredentials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
