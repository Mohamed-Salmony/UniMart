using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniMart_App.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFacultyAcademicYearToSingleRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademicYearId",
                table: "Faculties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_AcademicYearId",
                table: "Faculties",
                column: "AcademicYearId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faculties_AcademicYears_AcademicYearId",
                table: "Faculties",
                column: "AcademicYearId",
                principalTable: "AcademicYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faculties_AcademicYears_AcademicYearId",
                table: "Faculties");

            migrationBuilder.DropIndex(
                name: "IX_Faculties_AcademicYearId",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "Faculties");
        }
    }
}
