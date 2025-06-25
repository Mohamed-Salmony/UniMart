using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniMart_App.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFacultyToUseMaxAcademicYears : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacultyAcademicYears");

            migrationBuilder.AddColumn<int>(
                name: "MaxAcademicYears",
                table: "Faculties",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxAcademicYears",
                table: "Faculties");

            migrationBuilder.CreateTable(
                name: "FacultyAcademicYears",
                columns: table => new
                {
                    AcademicYearsId = table.Column<int>(type: "int", nullable: false),
                    FacultiesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyAcademicYears", x => new { x.AcademicYearsId, x.FacultiesId });
                    table.ForeignKey(
                        name: "FK_FacultyAcademicYears_AcademicYears_AcademicYearsId",
                        column: x => x.AcademicYearsId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacultyAcademicYears_Faculties_FacultiesId",
                        column: x => x.FacultiesId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacultyAcademicYears_FacultiesId",
                table: "FacultyAcademicYears",
                column: "FacultiesId");
        }
    }
}
