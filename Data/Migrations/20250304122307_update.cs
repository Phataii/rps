using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps.Data.Migrations
{
    public partial class update : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartmentName",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentName",
                table: "Results",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "FacultyBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DepartmentName",
                table: "FacultyBatches",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentName",
                table: "DepartmentBatches",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Session",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Session", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Results_Session",
                table: "Results",
                column: "Session");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyBatches_Session",
                table: "FacultyBatches",
                column: "Session");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentBatches_Session",
                table: "DepartmentBatches",
                column: "Session");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentBatches_Session_Session",
                table: "DepartmentBatches",
                column: "Session",
                principalTable: "Session",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FacultyBatches_Session_Session",
                table: "FacultyBatches",
                column: "Session",
                principalTable: "Session",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Results_Session_Session",
                table: "Results",
                column: "Session",
                principalTable: "Session",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentBatches_Session_Session",
                table: "DepartmentBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_FacultyBatches_Session_Session",
                table: "FacultyBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_Results_Session_Session",
                table: "Results");

            migrationBuilder.DropTable(
                name: "Session");

            migrationBuilder.DropIndex(
                name: "IX_Results_Session",
                table: "Results");

            migrationBuilder.DropIndex(
                name: "IX_FacultyBatches_Session",
                table: "FacultyBatches");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentBatches_Session",
                table: "DepartmentBatches");

            migrationBuilder.DropColumn(
                name: "DepartmentName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DepartmentName",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "FacultyBatches");

            migrationBuilder.DropColumn(
                name: "DepartmentName",
                table: "FacultyBatches");

            migrationBuilder.DropColumn(
                name: "DepartmentName",
                table: "DepartmentBatches");
        }
    }
}
