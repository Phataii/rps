using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps.Data.Migrations
{
    public partial class resultIdUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "DepartmentBatches");

            migrationBuilder.RenameColumn(
                name: "FacultyId",
                table: "DepartmentBatches",
                newName: "ResultId");

            migrationBuilder.AlterColumn<double>(
                name: "To",
                table: "Remarks",
                type: "double",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "From",
                table: "Remarks",
                type: "double",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResultId",
                table: "DepartmentBatches",
                newName: "FacultyId");

            migrationBuilder.AlterColumn<double>(
                name: "To",
                table: "Remarks",
                type: "double",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double");

            migrationBuilder.AlterColumn<double>(
                name: "From",
                table: "Remarks",
                type: "double",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "DepartmentBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
