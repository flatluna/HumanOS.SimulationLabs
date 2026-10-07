using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptEvaluationTokenUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EVL_CachedInputTokens",
                table: "LAB_AttemptEvaluation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EVL_InputTokens",
                table: "LAB_AttemptEvaluation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EVL_OutputTokens",
                table: "LAB_AttemptEvaluation",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EVL_CachedInputTokens",
                table: "LAB_AttemptEvaluation");

            migrationBuilder.DropColumn(
                name: "EVL_InputTokens",
                table: "LAB_AttemptEvaluation");

            migrationBuilder.DropColumn(
                name: "EVL_OutputTokens",
                table: "LAB_AttemptEvaluation");
        }
    }
}
