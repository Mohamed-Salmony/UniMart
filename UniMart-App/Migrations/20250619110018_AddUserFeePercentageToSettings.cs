using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniMart_App.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFeePercentageToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UserFeePercentage",
                table: "Settings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserFeePercentage",
                table: "Settings");
        }
    }
}
