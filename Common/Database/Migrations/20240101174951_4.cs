using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class _4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE PROCEDURE dbo. UserStates_Set\r\n\t-- Add the parameters for the stored procedure here\r\n\t@UserId uniqueidentifier,\r\n\t@StateId int,\r\n\t@Data varchar(max) = NULL\r\nAS\r\nBEGIN\r\n\tSET NOCOUNT ON;\r\n\r\n    IF EXISTS (SELECT 1\r\n\t\t\t   FROM dbo.UserStates (NOLOCK)\r\n\t\t\t   WHERE UserId = @UserId)\r\n\tBEGIN\r\n\t\tUPDATE dbo.UserStates\r\n\t\tSET \r\n\t\t\tStateId = @StateId,\r\n\t\t\tData = @Data\r\n\tEND\r\n\tELSE\r\n\tBEGIN\r\n\t\tINSERT INTO dbo.UserStates (UserId, StateId, Data)\r\n\t\tVALUES (@UserId, @StateId, @Data)\r\n\tEND\r\nEND\r\nGO");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE dbo.UserStates_Set");
        }
    }
}
