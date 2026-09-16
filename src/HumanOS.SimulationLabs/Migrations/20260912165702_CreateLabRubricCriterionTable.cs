using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HumanOS.SimulationLabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateLabRubricCriterionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_LAB_Rubric_SEG_IdTenant_LAB_IdVersion_RUB_IdRubric",
                table: "LAB_Rubric",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "RUB_IdRubric" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_MOM_IdExpectedMoment",
                table: "LAB_ExpectedMoment",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "MOM_IdExpectedMoment" });

            migrationBuilder.CreateTable(
                name: "LAB_RubricCriterion",
                columns: table => new
                {
                    CRT_IdCriterion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RUB_IdRubric = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LAB_IdVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SEG_IdTenant = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OBJ_IdObjective = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MOM_IdExpectedMoment = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CRT_Codigo = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    CRT_Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CRT_Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CRT_TipoEvidencia = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    CRT_Peso = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    CRT_ScoreMinimoEsperado = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 7.00m),
                    CRT_EsCritico = table.Column<bool>(type: "bit", nullable: false),
                    CRT_IndicadoresPositivos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CRT_IndicadoresNegativos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CRT_ErrorCritico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CRT_RecomendacionBase = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CRT_Orden = table.Column<int>(type: "int", nullable: false),
                    CRT_Estatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "DRAFT"),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAB_RubricCriterion", x => x.CRT_IdCriterion);
                    table.CheckConstraint("CK_LAB_RubricCriterion_CreadoPor", "LEN(TRIM([CreadoPor])) > 0");
                    table.CheckConstraint("CK_LAB_RubricCriterion_CRT_Codigo", "LEN(TRIM([CRT_Codigo])) > 0");
                    table.CheckConstraint("CK_LAB_RubricCriterion_CRT_Descripcion", "LEN(TRIM([CRT_Descripcion])) > 0");
                    table.CheckConstraint("CK_LAB_RubricCriterion_CRT_Estatus", "[CRT_Estatus] IN ('DRAFT', 'ACTIVE', 'INACTIVE')");
                    table.CheckConstraint("CK_LAB_RubricCriterion_CRT_IndicadoresPositivos", "LEN(TRIM([CRT_IndicadoresPositivos])) > 0");
                    table.CheckConstraint("CK_LAB_RubricCriterion_CRT_Nombre", "LEN(TRIM([CRT_Nombre])) > 0");
                    table.CheckConstraint("CK_LAB_RubricCriterion_CRT_Orden", "[CRT_Orden] > 0");
                    table.CheckConstraint("CK_LAB_RubricCriterion_CRT_Peso", "[CRT_Peso] > 0");
                    table.CheckConstraint("CK_LAB_RubricCriterion_CRT_ScoreMinimoEsperado", "[CRT_ScoreMinimoEsperado] >= 1.00 AND [CRT_ScoreMinimoEsperado] <= 10.00");
                    table.CheckConstraint("CK_LAB_RubricCriterion_CRT_TipoEvidencia", "[CRT_TipoEvidencia] IN ('CONVERSATION', 'USER_ACTION', 'ARTIFACT', 'DECISION', 'SYSTEM_RESULT', 'MULTIPLE')");
                    table.ForeignKey(
                        name: "FK_LAB_RubricCriterion_LAB_ExpectedMoment_Tenant_Version_Moment",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.MOM_IdExpectedMoment },
                        principalTable: "LAB_ExpectedMoment",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "MOM_IdExpectedMoment" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_RubricCriterion_LAB_Objective_Tenant_Version_Objective",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.OBJ_IdObjective },
                        principalTable: "LAB_Objective",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_IdObjective" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LAB_RubricCriterion_LAB_Rubric_Tenant_Version_Rubric",
                        columns: x => new { x.SEG_IdTenant, x.LAB_IdVersion, x.RUB_IdRubric },
                        principalTable: "LAB_Rubric",
                        principalColumns: new[] { "SEG_IdTenant", "LAB_IdVersion", "RUB_IdRubric" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_RubricCriterion_Tenant_Version_Moment",
                table: "LAB_RubricCriterion",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "MOM_IdExpectedMoment" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_RubricCriterion_Tenant_Version_Objective",
                table: "LAB_RubricCriterion",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "OBJ_IdObjective" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_RubricCriterion_Tenant_Version_Rubric",
                table: "LAB_RubricCriterion",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "RUB_IdRubric" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_RubricCriterion_Tenant_Version_Rubric_EsCritico",
                table: "LAB_RubricCriterion",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "RUB_IdRubric", "CRT_EsCritico" });

            migrationBuilder.CreateIndex(
                name: "IX_LAB_RubricCriterion_Tenant_Version_Rubric_Estatus",
                table: "LAB_RubricCriterion",
                columns: new[] { "SEG_IdTenant", "LAB_IdVersion", "RUB_IdRubric", "CRT_Estatus" });

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_RubricCriterion_Tenant_Rubric_Codigo",
                table: "LAB_RubricCriterion",
                columns: new[] { "SEG_IdTenant", "RUB_IdRubric", "CRT_Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_LAB_RubricCriterion_Tenant_Rubric_Orden",
                table: "LAB_RubricCriterion",
                columns: new[] { "SEG_IdTenant", "RUB_IdRubric", "CRT_Orden" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAB_RubricCriterion");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LAB_Rubric_SEG_IdTenant_LAB_IdVersion_RUB_IdRubric",
                table: "LAB_Rubric");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_MOM_IdExpectedMoment",
                table: "LAB_ExpectedMoment");
        }
    }
}
