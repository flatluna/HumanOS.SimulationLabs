using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabAttemptEvaluationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LAB_AttemptEvaluation",
                columns: table => new
                {
                    EVL_IdEvaluation = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ATT_IdAttempt = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EVL_ScoreFinal = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    EVL_Resultado = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    EVL_Feedback = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EVL_StrengthsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EVL_GapsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EVL_RecommendedSkillsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EVL_CriteriaScoresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EVL_GeneratedModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_AttemptEvaluation", x => x.EVL_IdEvaluation);
                    table.CheckConstraint("CK_LAB_AttemptEvaluation_Resultado", "[EVL_Resultado] IN ('PASSED', 'PARTIAL', 'REPEAT_RECOMMENDED', 'NOT_COMPLETED', 'CRITICAL_FAILURE')");
                    table.CheckConstraint("CK_LAB_AttemptEvaluation_ScoreFinal", "[EVL_ScoreFinal] >= 1.00 AND [EVL_ScoreFinal] <= 10.00");
                });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_AttemptEvaluation_Tenant_Attempt",
                table: "LAB_AttemptEvaluation",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_AttemptEvaluation");
        }
    }
}
