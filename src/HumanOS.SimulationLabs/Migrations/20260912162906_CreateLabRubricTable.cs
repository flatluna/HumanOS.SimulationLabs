using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabRubricTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LAB_Rubric",
                columns: table => new
                {
                    RUB_IdRubric = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RUB_Codigo = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    RUB_Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RUB_Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RUB_TipoEvaluacion = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    RUB_EscalaMinima = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    RUB_EscalaMaxima = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    RUB_ScoreMinimoAprobacion = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    RUB_MetodoCalculo = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    RUB_RequiereEvidencia = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RUB_PermiteFallaCritica = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RUB_InstruccionesEvaluador = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RUB_Estatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    RUB_VigenciaDesde = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RUB_VigenciaHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_Rubric", x => x.RUB_IdRubric);
                    table.CheckConstraint("CK_LAB_Rubric_CreadoPor", "LEN(TRIM([CreadoPor])) > 0");
                    table.CheckConstraint("CK_LAB_Rubric_Escala", "[RUB_EscalaMaxima] > [RUB_EscalaMinima]");
                    table.CheckConstraint("CK_LAB_Rubric_RUB_Codigo", "LEN(TRIM([RUB_Codigo])) > 0");
                    table.CheckConstraint("CK_LAB_Rubric_RUB_Descripcion", "LEN(TRIM([RUB_Descripcion])) > 0");
                    table.CheckConstraint("CK_LAB_Rubric_RUB_EscalaMaxima", "[RUB_EscalaMaxima] = 10.00");
                    table.CheckConstraint("CK_LAB_Rubric_RUB_EscalaMinima", "[RUB_EscalaMinima] = 1.00");
                    table.CheckConstraint("CK_LAB_Rubric_RUB_Estatus", "[RUB_Estatus] IN ('DRAFT', 'APPROVED', 'PUBLISHED', 'RETIRED')");
                    table.CheckConstraint("CK_LAB_Rubric_RUB_InstruccionesEvaluador", "LEN(TRIM([RUB_InstruccionesEvaluador])) > 0");
                    table.CheckConstraint("CK_LAB_Rubric_RUB_MetodoCalculo", "[RUB_MetodoCalculo] IN ('WEIGHTED_AVERAGE', 'SIMPLE_AVERAGE', 'CRITICAL_GATE', 'WEIGHTED_WITH_CRITICAL_GATE')");
                    table.CheckConstraint("CK_LAB_Rubric_RUB_Nombre", "LEN(TRIM([RUB_Nombre])) > 0");
                    table.CheckConstraint("CK_LAB_Rubric_RUB_ScoreMinimoAprobacion", "[RUB_ScoreMinimoAprobacion] >= [RUB_EscalaMinima] AND [RUB_ScoreMinimoAprobacion] <= [RUB_EscalaMaxima]");
                    table.CheckConstraint("CK_LAB_Rubric_RUB_TipoEvaluacion", "[RUB_TipoEvaluacion] IN ('CONVERSATION', 'DESKTOP', 'HYBRID', 'ARTIFACT', 'MULTI_SOURCE')");
                    table.CheckConstraint("CK_LAB_Rubric_Vigencia", "([RUB_VigenciaHasta] IS NULL) OR ([RUB_VigenciaDesde] IS NULL) OR ([RUB_VigenciaHasta] >= [RUB_VigenciaDesde])");
                    table.ForeignKey(
                        name: "FK_LAB_Rubric_LAB_LabVersion_SEG_IdTenant_LAB_IdVersion",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion },
                        principalTable: "LAB_LabVersion",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Rubric_SEG_IdTenant_RUB_Estatus",
                table: "LAB_Rubric",
                columns: new[] { "SEG_IdTenant", "RUB_Estatus" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Rubric_Tenant_Tipo_Estatus",
                table: "LAB_Rubric",
                columns: new[] { "SEG_IdTenant", "RUB_TipoEvaluacion", "RUB_Estatus" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_Rubric_Tenant_Vigencias",
                table: "LAB_Rubric",
                columns: new[] { "SEG_IdTenant", "RUB_VigenciaDesde", "RUB_VigenciaHasta" });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_Rubric_SEG_IdTenant_LAB_IdVersion",
                table: "LAB_Rubric",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_Rubric_SEG_IdTenant_RUB_Codigo",
                table: "LAB_Rubric",
                columns: new[] { "SEG_IdTenant", "RUB_Codigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_Rubric");
        }
    }
}
