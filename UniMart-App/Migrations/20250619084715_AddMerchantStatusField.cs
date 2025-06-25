using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniMart_App.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantStatusField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MerchantStatus",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MerchantStatus",
                table: "AspNetUsers");
        }
    }
}
