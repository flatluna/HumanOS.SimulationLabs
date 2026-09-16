using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabUserActionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_LAB_Attempt_SEG_IdTenant_LAB_IdVersion_ATT_IdAttempt",
                table: "LAB_Attempt",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "ATT_IdAttempt" });

            migrationBuilder.CreateTable(
                name: "LAB_UserAction",
                columns: table => new
                {
                    ACT_IdUserAction = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ATT_IdAttempt = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ACT_NumeroSecuencia = table.Column<int>(type: "int", nullable: false),
                    ACT_Codigo = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    ACT_Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ACT_Tipo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ACT_Pagina = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ACT_Componente = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ACT_EntidadTipo = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    ACT_EntidadId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ACT_ValorAnterior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACT_ValorNuevo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACT_InputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACT_OutputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACT_Resultado = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ACT_MensajeResultado = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ACT_EraPermitida = table.Column<bool>(type: "bit", nullable: false),
                    ACT_EraEsperada = table.Column<bool>(type: "bit", nullable: false),
                    ACT_EsCritica = table.Column<bool>(type: "bit", nullable: false),
                    ACT_Severidad = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ACT_MotivoEvaluacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MOM_IdExpectedMoment = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ACT_FechaInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ACT_FechaFin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ACT_DuracionMs = table.Column<int>(type: "int", nullable: true),
                    ACT_Origen = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ACT_Estatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "FINAL"),
                    ACT_ErrorCodigo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ACT_ErrorDescripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_UserAction", x => x.ACT_IdUserAction);
                    table.CheckConstraint("CK_LAB_UserAction_ACT_Codigo", "LEN(TRIM([ACT_Codigo])) > 0");
                    table.CheckConstraint("CK_LAB_UserAction_ACT_DuracionMs", "([ACT_DuracionMs] IS NULL) OR ([ACT_DuracionMs] >= 0)");
                    table.CheckConstraint("CK_LAB_UserAction_ACT_Estatus", "[ACT_Estatus] IN ('STARTED', 'FINAL', 'REVERSED', 'EXCLUDED', 'ERROR')");
                    table.CheckConstraint("CK_LAB_UserAction_ACT_InputJson", "([ACT_InputJson] IS NULL) OR (ISJSON([ACT_InputJson]) = 1)");
                    table.CheckConstraint("CK_LAB_UserAction_ACT_Nombre", "LEN(TRIM([ACT_Nombre])) > 0");
                    table.CheckConstraint("CK_LAB_UserAction_ACT_NumeroSecuencia", "[ACT_NumeroSecuencia] > 0");
                    table.CheckConstraint("CK_LAB_UserAction_ACT_Origen", "[ACT_Origen] IN ('PARTICIPANT', 'SYSTEM', 'SIMULATION')");
                    table.CheckConstraint("CK_LAB_UserAction_ACT_OutputJson", "([ACT_OutputJson] IS NULL) OR (ISJSON([ACT_OutputJson]) = 1)");
                    table.CheckConstraint("CK_LAB_UserAction_ACT_Resultado", "[ACT_Resultado] IN ('SUCCESS', 'BLOCKED', 'WARNING', 'FAILED', 'CANCELLED', 'PENDING')");
                    table.CheckConstraint("CK_LAB_UserAction_ACT_Severidad", "[ACT_Severidad] IN ('INFO', 'LOW', 'MEDIUM', 'HIGH', 'CRITICAL')");
                    table.CheckConstraint("CK_LAB_UserAction_ACT_Tipo", "[ACT_Tipo] IN ('NAVIGATION', 'VIEW', 'SEARCH', 'CREATE', 'UPDATE', 'SELECT', 'APPROVE', 'REJECT', 'ESCALATE', 'SUBMIT', 'DOWNLOAD', 'UPLOAD', 'DECISION', 'SYSTEM_COMMAND')");
                    table.CheckConstraint("CK_LAB_UserAction_CreadoPor", "LEN(TRIM([CreadoPor])) > 0");
                    table.CheckConstraint("CK_LAB_UserAction_Fechas", "([ACT_FechaFin] IS NULL) OR ([ACT_FechaFin] >= [ACT_FechaInicio])");
                    table.ForeignKey(
                        name: "FK_LAB_UserAction_LAB_Attempt_SEG_IdTenant_LAB_IdVersion_ATT_IdAttempt",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.ATT_IdAttempt },
                        principalTable: "LAB_Attempt",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "ATT_IdAttempt" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_UserAction_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_MOM_IdExpectedMoment",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.MOM_IdExpectedMoment },
                        principalTable: "LAB_ExpectedMoment",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "MOM_IdExpectedMoment" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_UserAction_SEG_IdTenant_ACT_EntidadTipo_ACT_EntidadId",
                table: "LAB_UserAction",
                columns: new[] { "SEG_IdTenant", "ACT_EntidadTipo", "ACT_EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_UserAction_SEG_IdTenant_ATT_IdAttempt_ACT_EsCritica",
                table: "LAB_UserAction",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt", "ACT_EsCritica" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_UserAction_SEG_IdTenant_ATT_IdAttempt_ACT_FechaInicio",
                table: "LAB_UserAction",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt", "ACT_FechaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_UserAction_SEG_IdTenant_ATT_IdAttempt_ACT_Resultado",
                table: "LAB_UserAction",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt", "ACT_Resultado" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_UserAction_SEG_IdTenant_LAB_IdVersion_ATT_IdAttempt",
                table: "LAB_UserAction",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "ATT_IdAttempt" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_UserAction_SEG_IdTenant_LAB_IdVersion_MOM_IdExpectedMoment",
                table: "LAB_UserAction",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "MOM_IdExpectedMoment" });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_UserAction_SEG_IdTenant_ATT_IdAttempt_ACT_NumeroSecuencia",
                table: "LAB_UserAction",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt", "ACT_NumeroSecuencia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_UserAction");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LAB_Attempt_SEG_IdTenant_LAB_IdVersion_ATT_IdAttempt",
                table: "LAB_Attempt");
        }
    }
}
