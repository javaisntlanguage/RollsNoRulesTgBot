using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class _17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminStates_AdminCredentials_AdminId",
                table: "AdminStates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdminStates",
                table: "AdminStates");

            migrationBuilder.RenameColumn(
                name: "AdminId",
                table: "AdminStates",
                newName: "AdminCredentialId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdminStates",
                table: "AdminStates",
                column: "UserId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminStates_AdminCredentials_AdminCredentialId",
                table: "AdminStates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdminStates",
                table: "AdminStates");

            migrationBuilder.DropIndex(
                name: "IX_AdminStates_AdminCredentialId",
                table: "AdminStates");

            migrationBuilder.RenameColumn(
                name: "AdminCredentialId",
                table: "AdminStates",
                newName: "AdminId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdminStates",
                table: "AdminStates",
                columns: new[] { "AdminId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AdminStates_AdminCredentials_AdminId",
                table: "AdminStates",
                column: "AdminId",
                principalTable: "AdminCredentials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
