using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class _3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE PROCEDURE dbo.User_Add\r\n\t-- Add the parameters for the stored procedure here\r\n\t@ChatId bigint,\r\n\t@TelegramUserName varchar(32)\r\nAS\r\nBEGIN\r\n\t-- SET NOCOUNT ON added to prevent extra result sets from\r\n\t-- interfering with SELECT statements.\r\n\tSET NOCOUNT ON;\r\n\r\n    INSERT INTO dbo.Users\r\n\tVALUES (NEWID(), @ChatId, @TelegramUserName)\r\nEND");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE dbo.User_Add");
        }
    }
}
