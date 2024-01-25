using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class _5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint("PK_UserStates", "UserStates");
            migrationBuilder.DropUniqueConstraint("FK_UserStates_Users_UserId", "UserStates");
            migrationBuilder.DropUniqueConstraint("FK_Addresses_Users_UserId", "Addresses");
            migrationBuilder.DropIndex("IX_Addresses_UserId", "Addresses");
            migrationBuilder.DropUniqueConstraint("PK_Users", "Users");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Users");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Users",
                type: "bigint",
                nullable: false);

            migrationBuilder.AddPrimaryKey("PK_Users", "Users", "Id");

            migrationBuilder.DropColumn(
                name: "ChatId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramUserName",
                table: "Users");

            /*migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "Users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");*/

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserStates");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "UserStates",
                type: "bigint",
                nullable: false);

            migrationBuilder.AddPrimaryKey("PK_UserStates", "UserStates", "UserId");
            migrationBuilder.AddForeignKey("FK_UserStates_Users_UserId", "UserStates", "UserId", "Users", principalColumn:"Id");

            /*migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "UserStates",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");*/

            /*migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Addresses",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");*/

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Addresses");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Addresses",
                type: "bigint",
                nullable: false);

            migrationBuilder.DropUniqueConstraint("PK_Addresses", "Addresses");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Addresses");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Addresses",
                type: "bigint",
                nullable: false);

            /*migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "Addresses",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");*/

            migrationBuilder.AddPrimaryKey("PK_Addresses", "Addresses", "Id");

            migrationBuilder.AddForeignKey("FK_Addresses_Users_UserId", "Addresses", "UserId", "Users", principalColumn:"Id");

            migrationBuilder.Sql("ALTER PROCEDURE [dbo].[User_Add]\r\n\t-- Add the parameters for the stored procedure here\r\n\t@Id bigint\r\nAS\r\nBEGIN\r\n\t-- SET NOCOUNT ON added to prevent extra result sets from\r\n\t-- interfering with SELECT statements.\r\n\tSET NOCOUNT ON;\r\n    INSERT INTO dbo.Users (Id)\r\n\tVALUES (@Id)\r\nEND");
            migrationBuilder.Sql("ALTER PROCEDURE [dbo].[UserStates_Set]\r\n\t-- Add the parameters for the stored procedure here\r\n\t@UserId bigint,\r\n\t@StateId int,\r\n\t@Data varchar(max) = NULL\r\nAS\r\nBEGIN\r\n\tSET NOCOUNT ON;\r\n    IF EXISTS (SELECT 1\r\n\t\t\t   FROM dbo.UserStates (NOLOCK)\r\n\t\t\t   WHERE UserId = @UserId)\r\n\tBEGIN\r\n\t\tUPDATE dbo.UserStates\r\n\t\tSET \r\n\t\t\tStateId = @StateId,\r\n\t\t\tData = @Data\r\n\tEND\r\n\tELSE\r\n\tBEGIN\r\n\t\tINSERT INTO dbo.UserStates (UserId, StateId, Data)\r\n\t\tVALUES (@UserId, @StateId, @Data)\r\n\tEND\r\nEND");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "UserStates",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<long>(
                name: "ChatId",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "TelegramUserName",
                table: "Users",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Addresses",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Addresses",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.Sql("ALTER PROCEDURE dbo. UserStates_Set\r\n\t-- Add the parameters for the stored procedure here\r\n\t@UserId uniqueidentifier,\r\n\t@StateId int,\r\n\t@Data varchar(max) = NULL\r\nAS\r\nBEGIN\r\n\tSET NOCOUNT ON;\r\n\r\n    IF EXISTS (SELECT 1\r\n\t\t\t   FROM dbo.UserStates (NOLOCK)\r\n\t\t\t   WHERE UserId = @UserId)\r\n\tBEGIN\r\n\t\tUPDATE dbo.UserStates\r\n\t\tSET \r\n\t\t\tStateId = @StateId,\r\n\t\t\tData = @Data\r\n\tEND\r\n\tELSE\r\n\tBEGIN\r\n\t\tINSERT INTO dbo.UserStates (UserId, StateId, Data)\r\n\t\tVALUES (@UserId, @StateId, @Data)\r\n\tEND\r\nEND\r\nGO");
            migrationBuilder.Sql("ALTER PROCEDURE dbo.User_Add\r\n\t-- Add the parameters for the stored procedure here\r\n\t@ChatId bigint,\r\n\t@TelegramUserName varchar(32)\r\nAS\r\nBEGIN\r\n\t-- SET NOCOUNT ON added to prevent extra result sets from\r\n\t-- interfering with SELECT statements.\r\n\tSET NOCOUNT ON;\r\n\r\n    INSERT INTO dbo.Users\r\n\tVALUES (NEWID(), @ChatId, @TelegramUserName)\r\nEND");

        }
    }
}
