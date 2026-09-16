using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabSimulatedActorTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_LAB_Scenario_SEG_IdTenant_LAB_IdVersion_SCN_IdScenario",
                table: "LAB_Scenario",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "SCN_IdScenario" });

            migrationBuilder.CreateTable(
                name: "LAB_SimulatedActor",
                columns: table => new
                {
                    ACT_IdActor = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SCN_IdScenario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ACT_Codigo = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    ACT_Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ACT_Rol = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ACT_Tipo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ACT_Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ACT_Objetivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ACT_ContextoConocido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ACT_BriefOculto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ACT_InformacionPuedeRevelar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACT_InformacionNoRevelarAutomaticamente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACT_Restricciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACT_Objeciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACT_Contradicciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACT_EstiloComunicacion = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ACT_NivelConocimiento = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ACT_Idioma = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ACT_VoiceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ACT_MensajeInicial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACT_PuedeIniciarConversacion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ACT_EsPrincipal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ACT_Orden = table.Column<int>(type: "int", nullable: false),
                    ACT_Estatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_SimulatedActor", x => x.ACT_IdActor);
                    table.CheckConstraint("CK_LAB_SimulatedActor_ACT_Estatus", "[ACT_Estatus] IN ('DRAFT', 'ACTIVE', 'INACTIVE')");
                    table.CheckConstraint("CK_LAB_SimulatedActor_ACT_EstiloComunicacion", "[ACT_EstiloComunicacion] IN ('DIRECT', 'COLLABORATIVE', 'RESERVED', 'SKEPTICAL', 'IMPATIENT', 'DETAIL_ORIENTED', 'EXECUTIVE')");
                    table.CheckConstraint("CK_LAB_SimulatedActor_ACT_NivelConocimiento", "[ACT_NivelConocimiento] IN ('LOW', 'MEDIUM', 'HIGH', 'EXPERT')");
                    table.CheckConstraint("CK_LAB_SimulatedActor_ACT_Orden", "[ACT_Orden] > 0");
                    table.CheckConstraint("CK_LAB_SimulatedActor_ACT_Tipo", "[ACT_Tipo] IN ('CLIENT', 'STAKEHOLDER', 'MANAGER', 'SUBJECT_MATTER_EXPERT', 'APPROVER', 'SYSTEM_OPERATOR', 'OBSERVER')");
                    table.ForeignKey(
                        name: "FK_LAB_SimulatedActor_LAB_Scenario_SEG_IdTenant_LAB_IdVersion_SCN_IdScenario",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.SCN_IdScenario },
                        principalTable: "LAB_Scenario",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "SCN_IdScenario" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_SimulatedActor_SEG_IdTenant_LAB_IdVersion_SCN_IdScenario",
                table: "LAB_SimulatedActor",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "SCN_IdScenario" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_SimulatedActor_SEG_IdTenant_SCN_IdScenario_ACT_EsPrincipal",
                table: "LAB_SimulatedActor",
                columns: new[] { "SEG_IdTenant", "SCN_IdScenario", "ACT_EsPrincipal" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_SimulatedActor_SEG_IdTenant_SCN_IdScenario_ACT_Estatus",
                table: "LAB_SimulatedActor",
                columns: new[] { "SEG_IdTenant", "SCN_IdScenario", "ACT_Estatus" });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_SimulatedActor_SEG_IdTenant_SCN_IdScenario_ACT_Codigo",
                table: "LAB_SimulatedActor",
                columns: new[] { "SEG_IdTenant", "SCN_IdScenario", "ACT_Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_SimulatedActor_SEG_IdTenant_SCN_IdScenario_ACT_Orden",
                table: "LAB_SimulatedActor",
                columns: new[] { "SEG_IdTenant", "SCN_IdScenario", "ACT_Orden" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_SimulatedActor");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LAB_Scenario_SEG_IdTenant_LAB_IdVersion_SCN_IdScenario",
                table: "LAB_Scenario");
        }
    }
}
