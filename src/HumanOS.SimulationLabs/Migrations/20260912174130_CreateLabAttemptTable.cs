using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabAttemptTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LAB_Attempt",
                columns: table => new
                {
                    ATT_IdAttempt = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SCN_IdScenario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    USR_IdParticipant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ATT_NumeroIntento = table.Column<int>(type: "int", nullable: false),
                    ATT_Estatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "NOT_STARTED"),
                    ATT_FechaInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ATT_FechaFin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ATT_DuracionSegundos = table.Column<int>(type: "int", nullable: true),
                    ATT_Modalidad = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ATT_SeedEjecucion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ATT_HashConfiguracion = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    ATT_ScoreFinal = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ATT_Resultado = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    ATT_Idioma = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ATT_UsaVoz = table.Column<bool>(type: "bit", nullable: false),
                    ATT_UsaEscritorio = table.Column<bool>(type: "bit", nullable: false),
                    ATT_UsaArtefactos = table.Column<bool>(type: "bit", nullable: false),
                    ATT_ErrorCodigo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ATT_ErrorDescripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_Attempt", x => x.ATT_IdAttempt);
                    table.CheckConstraint("CK_LAB_Attempt_ATT_DuracionSegundos", "([ATT_DuracionSegundos] IS NULL) OR ([ATT_DuracionSegundos] >= 0)");
                    table.CheckConstraint("CK_LAB_Attempt_ATT_Estatus", "[ATT_Estatus] IN ('NOT_STARTED', 'IN_PROGRESS', 'PAUSED', 'COMPLETED', 'ABANDONED', 'EVALUATING', 'EVALUATED', 'CANCELLED', 'ERROR')");
                    table.CheckConstraint("CK_LAB_Attempt_ATT_HashConfiguracion", "([ATT_HashConfiguracion] IS NULL) OR (LEN([ATT_HashConfiguracion]) = 64)");
                    table.CheckConstraint("CK_LAB_Attempt_ATT_Modalidad", "[ATT_Modalidad] IN ('VOICE', 'DESKTOP', 'HYBRID')");
                    table.CheckConstraint("CK_LAB_Attempt_ATT_NumeroIntento", "[ATT_NumeroIntento] > 0");
                    table.CheckConstraint("CK_LAB_Attempt_ATT_Resultado", "([ATT_Resultado] IS NULL) OR ([ATT_Resultado] IN ('PASSED', 'PARTIAL', 'REPEAT_RECOMMENDED', 'NOT_COMPLETED', 'CRITICAL_FAILURE'))");
                    table.CheckConstraint("CK_LAB_Attempt_ATT_ScoreFinal", "([ATT_ScoreFinal] IS NULL) OR ([ATT_ScoreFinal] >= 1.00 AND [ATT_ScoreFinal] <= 10.00)");
                    table.CheckConstraint("CK_LAB_Attempt_Fechas", "([ATT_FechaFin] IS NULL) OR ([ATT_FechaInicio] IS NULL) OR ([ATT_FechaFin] >= [ATT_FechaInicio])");
                    table.ForeignKey(
                        name: "FK_LAB_Attempt_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion },
                        principalTable: "LAB_LabVersion",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_Attempt_LAB_Scenario_SEG_IdTenant_LAB_IdVersion_SCN_IdScenario",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.SCN_IdScenario },
                        principalTable: "LAB_Scenario",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "SCN_IdScenario" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Attempt_SEG_IdTenant_LAB_IdVersion",
                table: "LAB_Attempt",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Attempt_SEG_IdTenant_LAB_IdVersion_SCN_IdScenario",
                table: "LAB_Attempt",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "SCN_IdScenario" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Attempt_SEG_IdTenant_SCN_IdScenario_ATT_Estatus",
                table: "LAB_Attempt",
                columns: new[] { "SEG_IdTenant", "SCN_IdScenario", "ATT_Estatus" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Attempt_SEG_IdTenant_USR_IdParticipant_ATT_Estatus",
                table: "LAB_Attempt",
                columns: new[] { "SEG_IdTenant", "USR_IdParticipant", "ATT_Estatus" });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_Attempt_SEG_IdTenant_SCN_IdScenario_USR_IdParticipant_ATT_NumeroIntento",
                table: "LAB_Attempt",
                columns: new[] { "SEG_IdTenant", "SCN_IdScenario", "USR_IdParticipant", "ATT_NumeroIntento" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_Attempt");
        }
    }
}
