using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class _6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER PROCEDURE [dbo].[UserStates_Set]\r\n\t@UserId bigint,\r\n\t@StateId int,\r\n\t@Data varchar(max) = NULL\r\nAS\r\nBEGIN\r\n\tSET NOCOUNT ON;\r\n\r\n\tIF NOT EXISTS (SELECT 1\r\n\t\t\t\t   FROM dbo.Users (NOLOCK)\r\n\t\t\t\t   WHERE Id = @UserId)\r\n\tBEGIN\r\n\t\tINSERT INTO dbo.Users (Id)\r\n\t\tVALUES (@UserId)\r\n\tEND\r\n\r\n    IF EXISTS (SELECT 1\r\n\t\t\t   FROM dbo.UserStates (NOLOCK)\r\n\t\t\t   WHERE UserId = @UserId)\r\n\tBEGIN\r\n\t\tUPDATE dbo.UserStates\r\n\t\tSET \r\n\t\t\tStateId = @StateId,\r\n\t\t\tData = @Data\r\n\tEND\r\n\tELSE\r\n\tBEGIN\r\n\t\tINSERT INTO dbo.UserStates (UserId, StateId, Data)\r\n\t\tVALUES (@UserId, @StateId, @Data)\r\n\tEND\r\nEND");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER PROCEDURE [dbo].[UserStates_Set]\r\n\t-- Add the parameters for the stored procedure here\r\n\t@UserId bigint,\r\n\t@StateId int,\r\n\t@Data varchar(max) = NULL\r\nAS\r\nBEGIN\r\n\tSET NOCOUNT ON;\r\n    IF EXISTS (SELECT 1\r\n\t\t\t   FROM dbo.UserStates (NOLOCK)\r\n\t\t\t   WHERE UserId = @UserId)\r\n\tBEGIN\r\n\t\tUPDATE dbo.UserStates\r\n\t\tSET \r\n\t\t\tStateId = @StateId,\r\n\t\t\tData = @Data\r\n\tEND\r\n\tELSE\r\n\tBEGIN\r\n\t\tINSERT INTO dbo.UserStates (UserId, StateId, Data)\r\n\t\tVALUES (@UserId, @StateId, @Data)\r\n\tEND\r\nEND");
        }
    }
}
