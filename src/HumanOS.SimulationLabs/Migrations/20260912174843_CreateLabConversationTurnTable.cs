using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabConversationTurnTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_LAB_SimulatedActor_SEG_IdTenant_ACT_IdActor",
                table: "LAB_SimulatedActor",
                columns: new[] { "SEG_IdTenant", "ACT_IdActor" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_LAB_Attempt_SEG_IdTenant_ATT_IdAttempt",
                table: "LAB_Attempt",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt" });

            migrationBuilder.CreateTable(
                name: "LAB_ConversationTurn",
                columns: table => new
                {
                    TRN_IdTurn = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ATT_IdAttempt = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TRN_NumeroTurno = table.Column<int>(type: "int", nullable: false),
                    TRN_SpeakerType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ACT_IdActor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TRN_Texto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TRN_TipoEntrada = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TRN_FechaInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TRN_FechaFin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TRN_DuracionMs = table.Column<int>(type: "int", nullable: true),
                    TRN_ConfianzaTranscripcion = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    TRN_FueInterrumpido = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TRN_InterrumpioTurnoAnterior = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TRN_EsRespuesta = table.Column<bool>(type: "bit", nullable: false),
                    TRN_IdTurnoRespondido = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TRN_IdExpectedMoment = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TRN_TextoFueEditado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TRN_TextoOriginal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TRN_EditadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TRN_FechaEdicion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TRN_Estatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "FINAL"),
                    TRN_ErrorCodigo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    TRN_ErrorDescripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_ConversationTurn", x => x.TRN_IdTurn);
                    table.CheckConstraint("CK_LAB_ConversationTurn_CreadoPor", "LEN(TRIM([CreadoPor])) > 0");
                    table.CheckConstraint("CK_LAB_ConversationTurn_Fechas", "([TRN_FechaFin] IS NULL) OR ([TRN_FechaFin] >= [TRN_FechaInicio])");
                    table.CheckConstraint("CK_LAB_ConversationTurn_TRN_ConfianzaTranscripcion", "([TRN_ConfianzaTranscripcion] IS NULL) OR ([TRN_ConfianzaTranscripcion] >= 0.0000 AND [TRN_ConfianzaTranscripcion] <= 1.0000)");
                    table.CheckConstraint("CK_LAB_ConversationTurn_TRN_DuracionMs", "([TRN_DuracionMs] IS NULL) OR ([TRN_DuracionMs] >= 0)");
                    table.CheckConstraint("CK_LAB_ConversationTurn_TRN_Estatus", "[TRN_Estatus] IN ('PARTIAL', 'FINAL', 'CORRECTED', 'EXCLUDED', 'ERROR')");
                    table.CheckConstraint("CK_LAB_ConversationTurn_TRN_NumeroTurno", "[TRN_NumeroTurno] > 0");
                    table.CheckConstraint("CK_LAB_ConversationTurn_TRN_SpeakerType", "[TRN_SpeakerType] IN ('PARTICIPANT', 'SIMULATED_ACTOR', 'SYSTEM')");
                    table.CheckConstraint("CK_LAB_ConversationTurn_TRN_Texto", "LEN(TRIM([TRN_Texto])) > 0");
                    table.CheckConstraint("CK_LAB_ConversationTurn_TRN_TipoEntrada", "[TRN_TipoEntrada] IN ('VOICE', 'TEXT', 'SYSTEM')");
                    table.ForeignKey(
                        name: "FK_LAB_ConversationTurn_LAB_Attempt_SEG_IdTenant_ATT_IdAttempt",
                        columns: x => new { x.SEG_IdTenant, x.ATT_IdAttempt },
                        principalTable: "LAB_Attempt",
                        principalColumns: new[] { "SEG_IdTenant", "ATT_IdAttempt" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_ConversationTurn_LAB_ConversationTurn_TRN_IdTurnoRespondido",
                        column: x => x.TRN_IdTurnoRespondido,
                        principalTable: "LAB_ConversationTurn",
                        principalColumn: "TRN_IdTurn",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_ConversationTurn_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_TRN_IdExpectedMoment",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.TRN_IdExpectedMoment },
                        principalTable: "LAB_ExpectedMoment",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "MOM_IdExpectedMoment" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_ConversationTurn_LAB_SimulatedActor_SEG_IdTenant_ACT_IdActor",
                        columns: x => new { x.SEG_IdTenant, x.ACT_IdActor },
                        principalTable: "LAB_SimulatedActor",
                        principalColumns: new[] { "SEG_IdTenant", "ACT_IdActor" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ConversationTurn_SEG_IdTenant_ACT_IdActor",
                table: "LAB_ConversationTurn",
                columns: new[] { "SEG_IdTenant", "ACT_IdActor" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ConversationTurn_SEG_IdTenant_ATT_IdAttempt_TRN_Estatus",
                table: "LAB_ConversationTurn",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt", "TRN_Estatus" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ConversationTurn_SEG_IdTenant_ATT_IdAttempt_TRN_FechaInicio",
                table: "LAB_ConversationTurn",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt", "TRN_FechaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ConversationTurn_SEG_IdTenant_ATT_IdAttempt_TRN_SpeakerType",
                table: "LAB_ConversationTurn",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt", "TRN_SpeakerType" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ConversationTurn_SEG_IdTenant_LAB_IdVersion_TRN_IdExpectedMoment",
                table: "LAB_ConversationTurn",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "TRN_IdExpectedMoment" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ConversationTurn_TRN_IdTurnoRespondido",
                table: "LAB_ConversationTurn",
                column: "TRN_IdTurnoRespondido");

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_ConversationTurn_SEG_IdTenant_ATT_IdAttempt_TRN_NumeroTurno",
                table: "LAB_ConversationTurn",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt", "TRN_NumeroTurno" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_ConversationTurn");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LAB_SimulatedActor_SEG_IdTenant_ACT_IdActor",
                table: "LAB_SimulatedActor");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LAB_Attempt_SEG_IdTenant_ATT_IdAttempt",
                table: "LAB_Attempt");
        }
    }
}
