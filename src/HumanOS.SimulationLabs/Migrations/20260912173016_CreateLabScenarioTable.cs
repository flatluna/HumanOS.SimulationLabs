using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabScenarioTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LAB_Scenario",
                columns: table => new
                {
                    SCN_IdScenario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SCN_Codigo = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    SCN_Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SCN_Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SCN_Tipo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    SCN_Dificultad = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    SCN_ContextoParticipante = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SCN_BriefOculto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SCN_ProblemaCentral = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SCN_ResultadoEsperado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SCN_CondicionesIniciales = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SCN_Restricciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SCN_Supuestos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SCN_Riesgos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SCN_InformacionNoRevelarAutomaticamente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SCN_MensajeInicial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SCN_DuracionSugeridaMinutos = table.Column<int>(type: "int", nullable: true),
                    SCN_PuntuacionObjetivo = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    SCN_PermiteReintento = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SCN_MaximoIntentos = table.Column<int>(type: "int", nullable: true),
                    SCN_UsaVariacion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SCN_SeedBase = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SCN_Estatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    SCN_VigenciaDesde = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SCN_VigenciaHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_Scenario", x => x.SCN_IdScenario);
                    table.CheckConstraint("CK_LAB_Scenario_SCN_Dificultad", "[SCN_Dificultad] IN ('BEGINNER', 'INTERMEDIATE', 'ADVANCED', 'EXPERT')");
                    table.CheckConstraint("CK_LAB_Scenario_SCN_DuracionSugeridaMinutos", "([SCN_DuracionSugeridaMinutos] IS NULL) OR ([SCN_DuracionSugeridaMinutos] > 0)");
                    table.CheckConstraint("CK_LAB_Scenario_SCN_Estatus", "[SCN_Estatus] IN ('DRAFT', 'APPROVED', 'PUBLISHED', 'RETIRED')");
                    table.CheckConstraint("CK_LAB_Scenario_SCN_MaximoIntentos", "([SCN_MaximoIntentos] IS NULL) OR ([SCN_MaximoIntentos] > 0)");
                    table.CheckConstraint("CK_LAB_Scenario_SCN_PuntuacionObjetivo", "([SCN_PuntuacionObjetivo] IS NULL) OR ([SCN_PuntuacionObjetivo] >= 1.00 AND [SCN_PuntuacionObjetivo] <= 10.00)");
                    table.CheckConstraint("CK_LAB_Scenario_SCN_Tipo", "[SCN_Tipo] IN ('CONVERSATIONAL', 'DESKTOP', 'HYBRID')");
                    table.CheckConstraint("CK_LAB_Scenario_Vigencia", "([SCN_VigenciaHasta] IS NULL) OR ([SCN_VigenciaDesde] IS NULL) OR ([SCN_VigenciaHasta] >= [SCN_VigenciaDesde])");
                    table.ForeignKey(
                        name: "FK_LAB_Scenario_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion },
                        principalTable: "LAB_LabVersion",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Scenario_SEG_IdTenant_LAB_IdVersion",
                table: "LAB_Scenario",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Scenario_SEG_IdTenant_LAB_IdVersion_SCN_Estatus",
                table: "LAB_Scenario",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "SCN_Estatus" });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_Scenario_SEG_IdTenant_LAB_IdVersion_SCN_Codigo",
                table: "LAB_Scenario",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "SCN_Codigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_Scenario");
        }
    }
}
