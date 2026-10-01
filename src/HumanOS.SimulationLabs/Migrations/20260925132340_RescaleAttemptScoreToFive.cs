using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class RescaleAttemptScoreToFive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LAB_AttemptEvaluation_ScoreFinal",
                table: "LAB_AttemptEvaluation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LAB_Attempt_ATT_ScoreFinal",
                table: "LAB_Attempt");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LAB_AttemptEvaluation_ScoreFinal",
                table: "LAB_AttemptEvaluation",
                sql: "[EVL_ScoreFinal] >= 1.00 AND [EVL_ScoreFinal] <= 5.00");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LAB_Attempt_ATT_ScoreFinal",
                table: "LAB_Attempt",
                sql: "([ATT_ScoreFinal] IS NULL) OR ([ATT_ScoreFinal] >= 1.00 AND [ATT_ScoreFinal] <= 5.00)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LAB_AttemptEvaluation_ScoreFinal",
                table: "LAB_AttemptEvaluation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LAB_Attempt_ATT_ScoreFinal",
                table: "LAB_Attempt");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LAB_AttemptEvaluation_ScoreFinal",
                table: "LAB_AttemptEvaluation",
                sql: "[EVL_ScoreFinal] >= 1.00 AND [EVL_ScoreFinal] <= 10.00");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LAB_Attempt_ATT_ScoreFinal",
                table: "LAB_Attempt",
                sql: "([ATT_ScoreFinal] IS NULL) OR ([ATT_ScoreFinal] >= 1.00 AND [ATT_ScoreFinal] <= 10.00)");
        }
    }
}
