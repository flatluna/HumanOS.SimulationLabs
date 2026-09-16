using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabArtifactSubmissionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LAB_ArtifactSubmission",
                columns: table => new
                {
                    SUB_IdSubmission = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ATT_IdAttempt = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    STG_IdStage = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OBJ_IdObjective = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SUB_CodigoArtefacto = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    SUB_Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SUB_Tipo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    SUB_Formato = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    SUB_Version = table.Column<int>(type: "int", nullable: false),
                    SUB_ContenidoTexto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SUB_ContenidoJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SUB_BlobPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SUB_NombreArchivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    SUB_MimeType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    SUB_HashSHA256 = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    SUB_Estatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    SUB_FechaInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SUB_FechaEnvio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SUB_EsEntregaFinal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SUB_RequiereEvaluacion = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SUB_FueGeneradoConAsistencia = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SUB_TipoAsistencia = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    SUB_ErrorCodigo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    SUB_ErrorDescripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_ArtifactSubmission", x => x.SUB_IdSubmission);
                    table.CheckConstraint("CK_LAB_ArtifactSubmission_CreadoPor", "LEN(TRIM([CreadoPor])) > 0");
                    table.CheckConstraint("CK_LAB_ArtifactSubmission_Fechas", "([SUB_FechaEnvio] IS NULL) OR ([SUB_FechaInicio] IS NULL) OR ([SUB_FechaEnvio] >= [SUB_FechaInicio])");
                    table.CheckConstraint("CK_LAB_ArtifactSubmission_SUB_CodigoArtefacto", "LEN(TRIM([SUB_CodigoArtefacto])) > 0");
                    table.CheckConstraint("CK_LAB_ArtifactSubmission_SUB_ContenidoJson", "([SUB_ContenidoJson] IS NULL) OR (ISJSON([SUB_ContenidoJson]) = 1)");
                    table.CheckConstraint("CK_LAB_ArtifactSubmission_SUB_Estatus", "[SUB_Estatus] IN ('DRAFT', 'SUBMITTED', 'FINAL', 'REPLACED', 'EXCLUDED', 'ERROR')");
                    table.CheckConstraint("CK_LAB_ArtifactSubmission_SUB_Formato", "[SUB_Formato] IN ('TEXT', 'MARKDOWN', 'HTML', 'JSON', 'STRUCTURED_FORM', 'FILE', 'DIAGRAM_DATA')");
                    table.CheckConstraint("CK_LAB_ArtifactSubmission_SUB_HashSHA256", "([SUB_HashSHA256] IS NULL) OR (LEN([SUB_HashSHA256]) = 64)");
                    table.CheckConstraint("CK_LAB_ArtifactSubmission_SUB_Nombre", "LEN(TRIM([SUB_Nombre])) > 0");
                    table.CheckConstraint("CK_LAB_ArtifactSubmission_SUB_Tipo", "[SUB_Tipo] IN ('FORM', 'WORKSHEET', 'MATRIX', 'DOCUMENT', 'DIAGRAM', 'DECISION_RECORD', 'CHECKLIST', 'JSON_ARTIFACT', 'FILE_REFERENCE')");
                    table.CheckConstraint("CK_LAB_ArtifactSubmission_SUB_TipoAsistencia", "([SUB_TipoAsistencia] IS NULL) OR ([SUB_TipoAsistencia] IN ('NONE', 'HINT', 'TEMPLATE', 'AI_ASSISTANCE', 'HUMAN_ASSISTANCE', 'SYSTEM_SUGGESTION'))");
                    table.CheckConstraint("CK_LAB_ArtifactSubmission_SUB_Version", "[SUB_Version] > 0");
                    table.ForeignKey(
                        name: "FK_LAB_ArtifactSubmission_LAB_Attempt_SEG_IdTenant_LAB_IdVersion_ATT_IdAttempt",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.ATT_IdAttempt },
                        principalTable: "LAB_Attempt",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "ATT_IdAttempt" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_ArtifactSubmission_LAB_Objective_SEG_IdTenant_LAB_IdVersion_OBJ_IdObjective",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.OBJ_IdObjective },
                        principalTable: "LAB_Objective",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_IdObjective" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_ArtifactSubmission_LAB_Stage_SEG_IdTenant_LAB_IdVersion_STG_IdStage",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.STG_IdStage },
                        principalTable: "LAB_Stage",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ArtifactSubmission_SEG_IdTenant_ATT_IdAttempt_SUB_EsEntregaFinal",
                table: "LAB_ArtifactSubmission",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt", "SUB_EsEntregaFinal" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ArtifactSubmission_SEG_IdTenant_ATT_IdAttempt_SUB_Estatus",
                table: "LAB_ArtifactSubmission",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt", "SUB_Estatus" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ArtifactSubmission_SEG_IdTenant_LAB_IdVersion_ATT_IdAttempt",
                table: "LAB_ArtifactSubmission",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "ATT_IdAttempt" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ArtifactSubmission_SEG_IdTenant_LAB_IdVersion_OBJ_IdObjective",
                table: "LAB_ArtifactSubmission",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_IdObjective" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ArtifactSubmission_SEG_IdTenant_LAB_IdVersion_STG_IdStage",
                table: "LAB_ArtifactSubmission",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "STG_IdStage" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_ArtifactSubmission_SEG_IdTenant_SUB_HashSHA256",
                table: "LAB_ArtifactSubmission",
                columns: new[] { "SEG_IdTenant", "SUB_HashSHA256" });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_ArtifactSubmission_SEG_IdTenant_ATT_IdAttempt_SUB_CodigoArtefacto_SUB_Version",
                table: "LAB_ArtifactSubmission",
                columns: new[] { "SEG_IdTenant", "ATT_IdAttempt", "SUB_CodigoArtefacto", "SUB_Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_ArtifactSubmission");
        }
    }
}
