using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class LoosenEnrollmentLabForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LAB_Enrollment_LAB_Lab_SEG_IdTenant_LAB_IdLab",
                table: "LAB_Enrollment");

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Enrollment_LAB_IdLab",
                table: "LAB_Enrollment",
                column: "LAB_IdLab");

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_Enrollment_LAB_Lab_LAB_IdLab",
                table: "LAB_Enrollment",
                column: "LAB_IdLab",
                principalTable: "LAB_Lab",
                principalColumn: "LAB_IdLab",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LAB_Enrollment_LAB_Lab_LAB_IdLab",
                table: "LAB_Enrollment");

            migrationBuilder.DropIndex(
                name: "IX_LAB_Enrollment_LAB_IdLab",
                table: "LAB_Enrollment");

            migrationBuilder.AddForeignKey(
                name: "FK_LAB_Enrollment_LAB_Lab_SEG_IdTenant_LAB_IdLab",
                table: "LAB_Enrollment",
                columns: new[] { "SEG_IdTenant", "LAB_IdLab" },
                principalTable: "LAB_Lab",
                principalColumns: new[] { "SEG_IdTenant", "LAB_IdLab" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
