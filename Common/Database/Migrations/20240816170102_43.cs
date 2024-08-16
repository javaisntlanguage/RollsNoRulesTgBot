using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class _43 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsGroup = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminPermissions",
                columns: table => new
                {
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    RightId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminPermissions", x => new { x.AdminId, x.RightId });
                    table.ForeignKey(
                        name: "FK_AdminPermissions_AdminCredentials_AdminId",
                        column: x => x.AdminId,
                        principalTable: "AdminCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminPermissions_Rights_RightId",
                        column: x => x.RightId,
                        principalTable: "Rights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupPermissions",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RightId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminPermissionAdminId = table.Column<int>(type: "int", nullable: true),
                    AdminPermissionRightId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPermissions", x => new { x.GroupId, x.RightId });
                    table.ForeignKey(
                        name: "FK_GroupPermissions_AdminPermissions_AdminPermissionAdminId_AdminPermissionRightId",
                        columns: x => new { x.AdminPermissionAdminId, x.AdminPermissionRightId },
                        principalTable: "AdminPermissions",
                        principalColumns: new[] { "AdminId", "RightId" });
                    table.ForeignKey(
                        name: "FK_GroupPermissions_Rights_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Rights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupPermissions_Rights_RightId",
                        column: x => x.RightId,
                        principalTable: "Rights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminPermissions_RightId",
                table: "AdminPermissions",
                column: "RightId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupPermissions_AdminPermissionAdminId_AdminPermissionRightId",
                table: "GroupPermissions",
                columns: new[] { "AdminPermissionAdminId", "AdminPermissionRightId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupPermissions_RightId",
                table: "GroupPermissions",
                column: "RightId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupPermissions");

            migrationBuilder.DropTable(
                name: "AdminPermissions");

            migrationBuilder.DropTable(
                name: "Rights");
        }
    }
}
