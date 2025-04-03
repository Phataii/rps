using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps.Data.Migrations
{
    public partial class departmentCodeupdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Department",
                table: "Users",
                newName: "DepartmentCode");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "Results",
                newName: "DepartmentCode");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "Grades",
                newName: "DepartmentCode");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "FacultyBatches",
                newName: "DepartmentCode");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "DepartmentBatches",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentCode",
                table: "DepartmentBatches",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DepartmentCode",
                table: "DepartmentBatches");

            migrationBuilder.RenameColumn(
                name: "DepartmentCode",
                table: "Users",
                newName: "Department");

            migrationBuilder.RenameColumn(
                name: "DepartmentCode",
                table: "Results",
                newName: "DepartmentId");

            migrationBuilder.RenameColumn(
                name: "DepartmentCode",
                table: "Grades",
                newName: "DepartmentId");

            migrationBuilder.RenameColumn(
                name: "DepartmentCode",
                table: "FacultyBatches",
                newName: "DepartmentId");

            migrationBuilder.AlterColumn<string>(
                name: "DepartmentId",
                table: "DepartmentBatches",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
