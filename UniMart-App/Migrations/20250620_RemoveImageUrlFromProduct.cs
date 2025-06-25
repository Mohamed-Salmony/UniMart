using Microsoft.EntityFrameworkCore.Migrations;

namespace UniMart_App.Migrations
{
    public partial class RemoveImageUrlFromProduct : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");
        }


    }
}
