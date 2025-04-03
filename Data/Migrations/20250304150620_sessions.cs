using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rps.Data.Migrations
{
    public partial class sessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropPrimaryKey(
                name: "PK_Session",
                table: "Session");

            migrationBuilder.RenameTable(
                name: "Session",
                newName: "Sessions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sessions",
                table: "Sessions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentBatches_Sessions_Session",
                table: "DepartmentBatches",
                column: "Session",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FacultyBatches_Sessions_Session",
                table: "FacultyBatches",
                column: "Session",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Results_Sessions_Session",
                table: "Results",
                column: "Session",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentBatches_Sessions_Session",
                table: "DepartmentBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_FacultyBatches_Sessions_Session",
                table: "FacultyBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_Results_Sessions_Session",
                table: "Results");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sessions",
                table: "Sessions");

            migrationBuilder.RenameTable(
                name: "Sessions",
                newName: "Session");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Session",
                table: "Session",
                column: "Id");

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
    }
}
