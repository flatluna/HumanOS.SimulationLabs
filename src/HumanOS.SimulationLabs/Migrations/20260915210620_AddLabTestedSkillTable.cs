using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class AddLabTestedSkillTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_LAB_RubricCriterion_SEG_IdTenant_LAB_IdVersion_CRT_IdCriterion",
                table: "LAB_RubricCriterion",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "CRT_IdCriterion" });

            migrationBuilder.CreateTable(
                name: "LAB_TestedSkill",
                columns: table => new
                {
                    SKL_IdSkill = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RUB_IdCriterion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SKL_Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SKL_Tipo = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    SKL_RelevanceToRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SKL_DemonstrationStandard = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SKL_FeedbackGuidance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_TestedSkill", x => x.SKL_IdSkill);
                    table.CheckConstraint("CK_LAB_TestedSkill_SKL_Tipo", "[SKL_Tipo] IN ('TECHNICAL', 'SOFT')");
                    table.ForeignKey(
                        name: "FK_LAB_TestedSkill_LAB_LabVersion_Tenant_Version",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion },
                        principalTable: "LAB_LabVersion",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_TestedSkill_LAB_RubricCriterion_Tenant_Version_Criterion",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.RUB_IdCriterion },
                        principalTable: "LAB_RubricCriterion",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "CRT_IdCriterion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_TestedSkill_Tenant_Version",
                table: "LAB_TestedSkill",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_TestedSkill_Tenant_Version_Criterion",
                table: "LAB_TestedSkill",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "RUB_IdCriterion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_TestedSkill");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LAB_RubricCriterion_SEG_IdTenant_LAB_IdVersion_CRT_IdCriterion",
                table: "LAB_RubricCriterion");
        }
    }
}
