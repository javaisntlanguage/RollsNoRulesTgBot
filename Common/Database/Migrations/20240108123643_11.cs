using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class _11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO Products(Name, Description, Price, IsVisible) " +
                "VALUES('Лонгролл креветка', 'Рис, нори, креветка, сливочный сыр, салат, огурец, соус спайси', 349, 1)");
            
            migrationBuilder.Sql("INSERT INTO ProductCategories(ProductId, CategoryId) " +
                "VALUES(1, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Products WHERE NAME = 'Лонгролл креветка'");
            migrationBuilder.Sql("DELETE FROM ProductCategories WHERE ProductId = 1 AND CategoryId = 1");
        }
    }
}
