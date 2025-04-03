using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps.Data.Migrations
{
    public partial class DeanStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "DepartmentBatches");

            migrationBuilder.AddColumn<string>(
                name: "DeanStatus",
                table: "DepartmentBatches",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "HODStatus",
                table: "DepartmentBatches",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeanStatus",
                table: "DepartmentBatches");

            migrationBuilder.DropColumn(
                name: "HODStatus",
                table: "DepartmentBatches");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DepartmentBatches",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
